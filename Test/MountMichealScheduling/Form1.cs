using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using ExcelDataReader;
using System.Collections;
using ClosedXML.Excel;
using System.Diagnostics;
using System.Threading;

namespace MountMichaelScheduling
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //Constant
        /*****************************************************************************************/
        int COST_CONFLICT_CLASS = 1000;

        int COST_DEPARTMENT = 1000;

        int COST_PAIR_UNSATISFIED = 1000;

        int COST_MAXIMUM_CAPACITY_EXCEEDED = 1000;
        int COST_RECOMMENDED_CAPACITY_EXCEEDED = 10;

        int COST_PRIORITY_0_DENIED = 1000;
        int COST_PRIORITY_1_DENIED = 50;
        int COST_PRIORITY_2_DENIED = 25;
        int COST_PRIORITY_3_DENIED = 15;
        int COST_PRIORITY_4_DENIED = 10;
        int COST_PRIORITY_5_DENIED = 5;

        int COST_CREDIT_INSUFFICIENT = 1000;

        int MAXIMUM_SEARCH_SECONDS = 120;

        //Variable
        /*****************************************************************************************/

        List<Student> students;
        List<Student> studentsError;
        Dictionary<int, Subject> subjects;
        Dictionary<int, Department> departments;
        Dictionary<int, Room> rooms;
        Dictionary<int, List<Class>> classes;
        List<PairRelation> pairRelations;
        List<AcceptableConflictRelation> acceptableConflictRelations;
        List<Tuple<int,int>> masterScheduleError;
        int[] credits;
        int iterationCount;

        List<List<int>> bestSolution, currentSolution;
        double bestCost, currentCost;
        bool solutionFound;

        Dictionary<int, int>[,,] studentCount;
        List<int>[,,,] schedules;
        int[] studentProgress;

        Stopwatch stopWatch;

        List<(int stuInd, int enrollInd)> backtrackOrder;

        //Thuat Sap Xep
        /*****************************************************************************************/

        private int GetSubjectId(int stuInd, int enrollInd)
        {
            Student student = students[stuInd];
            Enrollment enrollment = student.enrollments[enrollInd];
            return enrollment.subjectId;
        }

        // Return whether the student has registered a certain class at a certain period
        private bool IsEnrolled(List<int> solution, int stuInd, int classId)
        {
            for (int enrollInd = 0; enrollInd < solution.Count; ++enrollInd)
            {
                int classInd = solution[enrollInd];
                if (classInd == 0)
                    continue;

                int subjectId = GetSubjectId(stuInd, enrollInd);
                if (classId == classes[subjectId][classInd - 1].id)
                    return true;
            }
            return false;
        }

        private bool[] GetSemesterAsBool(int semester)
        {
            bool[] result = new bool[2];
            for (int i = 0; i < 2; ++i)
                result[i] = ((semester >> i) & 1) > 0;
            return result;
        }

        private bool IsAcceptableConflict(int classId1, int classId2)
        {
            foreach (AcceptableConflictRelation conflict in acceptableConflictRelations)
            {
                if (conflict.classId1 == classId1 && conflict.classId2 == classId2)
                    return true;
                if (conflict.classId1 == classId2 && conflict.classId2 == classId1)
                    return true;
            }
            return false;
        }

        private int GetCostFromPriority(int priority)
        {
            switch (priority)
            {
                case 0:
                    return COST_PRIORITY_0_DENIED;
                case 1:
                    return COST_PRIORITY_1_DENIED;
                case 2:
                    return COST_PRIORITY_2_DENIED;
                case 3:
                    return COST_PRIORITY_3_DENIED;
                case 4:
                    return COST_PRIORITY_4_DENIED;
                default:
                    return COST_PRIORITY_5_DENIED;
            }
        }

        private int CountConflictClassPair(List<List<int>> solution)
        {
            int result = 0;

            for (int stuInd = 0; stuInd < solution.Count; ++stuInd)
            {
                List<int>[,,] schedules = new List<int>[2, 5, 10];
                for (int semester = 0; semester < 2; ++semester)
                    for (int day = 0; day < 5; ++day)
                        for (int period = 0; period < 10; ++period)
                            schedules[semester, day, period] = new List<int>();

                for (int enrollInd = 0; enrollInd < solution[stuInd].Count; ++enrollInd)
                {
                    int classInd = solution[stuInd][enrollInd];
                    if (classInd == 0)
                        continue;

                    int subjectId = GetSubjectId(stuInd, enrollInd);
                    int period = classes[subjectId][classInd - 1].period;
                    bool[] daysOfWeek = classes[subjectId][classInd - 1].daysOfWeek;
                    bool[] semesters = GetSemesterAsBool(classes[subjectId][classInd - 1].semester);

                    for (int semester = 0; semester < 2; ++semester)
                    {
                        if (!semesters[semester])
                            continue;
                        for (int day = 0; day < 5; ++day)
                        {
                            if (!daysOfWeek[day])
                                continue;

                            int classId = classes[subjectId][classInd - 1].id;

                            foreach (int id in schedules[semester, day, period - 1])
                            {
                                //Check if the conflict is not acceptable
                                if (!IsAcceptableConflict(classId, id))
                                    ++result;
                            }

                            schedules[semester, day, period - 1].Add(classId);
                        }
                    }
                }
            }

            return result;
        }

        private int CountDepartmentBoundUnsatisfied(List<List<int>> solution)
        {
            int result = 0;

            for (int stuInd = 0; stuInd < solution.Count; ++stuInd)
            {
                Dictionary<int, int> enrollmentCounts = new Dictionary<int, int>();

                for (int enrollInd = 0; enrollInd < solution[stuInd].Count; ++enrollInd)
                {
                    int departmentId = students[stuInd].enrollments[enrollInd].departmentId;
                    if (!enrollmentCounts.ContainsKey(departmentId))
                        enrollmentCounts.Add(departmentId, 0);

                    int classInd = solution[stuInd][enrollInd];
                    if (classInd == 0)
                        continue;

                    ++enrollmentCounts[departmentId];
                }

                foreach (KeyValuePair<int, int> item in enrollmentCounts)
                {
                    result += Math.Max(0, Math.Max(departments[item.Key].minBound - item.Value, item.Value - departments[item.Key].maxBound));
                }
            }

            return result;
        }

        private int CountPairRelationUnsatisfied(List<List<int>> solution)
        {
            int result = 0;

            for (int stuInd = 0; stuInd < solution.Count; ++stuInd)
            {
                foreach (PairRelation pair in pairRelations)
                {
                    bool enrolled1 = IsEnrolled(solution[stuInd], stuInd, pair.classId1);
                    bool enrolled2 = IsEnrolled(solution[stuInd], stuInd, pair.classId2);
                    if (enrolled1 ^ enrolled2)
                        ++result;
                }
            }

            return result;
        }

        private int[] CountRoomCapacityExceeded(List<List<int>> solution)
        {
            Dictionary<int, int>[,,] studentCount = new Dictionary<int, int>[2, 5, 10];
            for (int semester = 0; semester < 2; ++semester)
            {
                for (int day = 0; day < 5; ++day)
                {
                    for (int period = 0; period < 10; ++period)
                    {
                        studentCount[semester, day, period] = new Dictionary<int, int>();
                        foreach (int item in rooms.Keys)
                        {
                            studentCount[semester, day, period].Add(item, 0);
                        }
                    }
                }
            }

            for (int stuInd = 0; stuInd < solution.Count; ++stuInd)
            {
                for (int enrollInd = 0; enrollInd < solution[stuInd].Count; ++enrollInd)
                {
                    int classInd = solution[stuInd][enrollInd];
                    if (classInd == 0)
                        continue;

                    int subjectId = GetSubjectId(stuInd, enrollInd);
                    int period = classes[subjectId][classInd - 1].period;
                    int roomId = classes[subjectId][classInd - 1].roomId;
                    bool[] daysOfWeek = classes[subjectId][classInd - 1].daysOfWeek;
                    bool[] semesters = GetSemesterAsBool(classes[subjectId][classInd - 1].semester);

                    for (int semester = 0; semester < 2; ++semester)
                        for (int day = 0; day < 5; ++day)
                            if (semesters[semester] && daysOfWeek[day])
                                ++studentCount[semester, day, period - 1][roomId];
                }
            }

            int[] result = new int[2];
            //result[0]: recommended capacity exceeded
            //result[1]: maximum capacity exceeded

            foreach (int roomId in rooms.Keys)
            {
                int recommendedCapacity = rooms[roomId].recommendedCapacity;
                int maximumCapacity = rooms[roomId].maximumCapacity;

                for (int semester = 0; semester < 2; ++semester)
                {
                    for (int day = 0; day < 5; ++day)
                    {
                        for (int period = 0; period < 10; ++period)
                        {
                            int x = studentCount[semester, day, period][roomId];
                            if (x > maximumCapacity)
                            {
                                result[1] += x - maximumCapacity;
                                result[0] += maximumCapacity - recommendedCapacity;
                            }
                            else
                                result[0] += Math.Max(0, x - recommendedCapacity);
                        }
                    }
                }
            }

            return result;
        }

        private int[] CountEnrollmentDenied(List<List<int>> solution)
        {
            int[] result = new int[6];
            //result[i]: number of enrollment with priority i that is denied

            for (int stuInd = 0; stuInd < solution.Count; ++stuInd)
            {
                for (int enrollInd = 0; enrollInd < solution[stuInd].Count; ++enrollInd)
                {
                    if (solution[stuInd][enrollInd] > 0)
                        continue;

                    int priority = students[stuInd].enrollments[enrollInd].priority;
                    priority = Math.Min(priority, 5);
                    ++result[priority];
                }
            }

            return result;
        }

        private int CountCreditInsufficient(List<List<int>> solution)
        {
            int result = 0;

            for (int stuInd = 0; stuInd < solution.Count; ++stuInd)
            {
                int creditCount = 0;

                for (int enrollInd = 0; enrollInd < solution[stuInd].Count; ++enrollInd)
                {
                    int subjectId = GetSubjectId(stuInd, enrollInd);
                    int classInd = solution[stuInd][enrollInd];
                    if (classInd == 0)
                        continue;

                    int classId = classes[subjectId][classInd - 1].id;
                    bool[] daysOfWeek = classes[subjectId][classInd - 1].daysOfWeek;
                    bool[] semesters = GetSemesterAsBool(classes[subjectId][classInd - 1].semester);

                    int dayOfWeekCount = 0;
                    for (int day = 0; day < 5; ++day)
                        if (daysOfWeek[day])
                            ++dayOfWeekCount;

                    int semesterCount = 0;
                    for (int semester = 0; semester < 2; ++semester)
                        if (semesters[semester])
                            ++semesterCount;

                    creditCount += semesterCount * dayOfWeekCount;
                }

                int requiredCredit = credits[students[stuInd].grade - 9];

                result += Math.Max(0, requiredCredit - creditCount);
            }

            return result;
        }

        private bool IsValidForStudent(int stuInd, List<int> solution)
        {
            //Check if the sum of credit for accepted enrollment is sufficient
            int creditCount = 0;

            for (int enrollInd = 0; enrollInd < solution.Count; ++enrollInd)
            {
                int subjectId = GetSubjectId(stuInd, enrollInd);
                int classInd = solution[enrollInd];
                if (classInd == 0)
                    continue;

                int classId = classes[subjectId][classInd - 1].id;
                bool[] daysOfWeek = classes[subjectId][classInd - 1].daysOfWeek;
                bool[] semesters = GetSemesterAsBool(classes[subjectId][classInd - 1].semester);

                int dayOfWeekCount = 0;
                for (int day = 0; day < 5; ++day)
                    if (daysOfWeek[day])
                        ++dayOfWeekCount;

                int semesterCount = 0;
                for (int semester = 0; semester < 2; ++semester)
                    if (semesters[semester])
                        ++semesterCount;

                creditCount += semesterCount * dayOfWeekCount;
            }

            int requiredCredit = credits[students[stuInd].grade - 9];
            if (creditCount < requiredCredit)
                return false;

            //Check if each department has correct number of subject registered
            Dictionary<int, int> enrollmentCounts = new Dictionary<int, int>();

            for (int enrollInd = 0; enrollInd < solution.Count; ++enrollInd)
            {
                int departmentId = students[stuInd].enrollments[enrollInd].departmentId;
                if (!enrollmentCounts.ContainsKey(departmentId))
                    enrollmentCounts.Add(departmentId, 0);

                int classInd = solution[enrollInd];
                if (classInd == 0)
                    continue;

                ++enrollmentCounts[departmentId];
            }

            foreach (KeyValuePair<int, int> item in enrollmentCounts)
            {
                if (item.Value < departments[item.Key].minBound || item.Value > departments[item.Key].maxBound)
                    return false;
            }

            //foreach (int key in departments.Keys)
            //{
            //    int value = enrollmentCounts.ContainsKey(key) ? enrollmentCounts[key] : 0;
            //    if (value < departments[key].minBound || value > departments[key].maxBound)
            //        return false;
            //}

            //Check if pair of classes that come together are both registered or both denied
            foreach (PairRelation pair in pairRelations)
            {
                bool enrolled1 = IsEnrolled(solution, stuInd, pair.classId1);
                bool enrolled2 = IsEnrolled(solution, stuInd, pair.classId2);
                if (enrolled1 ^ enrolled2)
                    return false;
            }

            return true;
        }

        private double GetSolutionCost(List<List<int>> solution)
        {
            double result = 0;

            result += CountConflictClassPair(solution) * COST_CONFLICT_CLASS;

            result += CountDepartmentBoundUnsatisfied(solution) * COST_DEPARTMENT;

            int[] roomCapacityExceeded = CountRoomCapacityExceeded(solution);
            result += roomCapacityExceeded[0] * COST_RECOMMENDED_CAPACITY_EXCEEDED;
            result += roomCapacityExceeded[1] * COST_MAXIMUM_CAPACITY_EXCEEDED;

            int[] enrollmentsDenied = CountEnrollmentDenied(solution);
            result += enrollmentsDenied[0] * COST_PRIORITY_0_DENIED;
            result += enrollmentsDenied[1] * COST_PRIORITY_1_DENIED;
            result += enrollmentsDenied[2] * COST_PRIORITY_2_DENIED;
            result += enrollmentsDenied[3] * COST_PRIORITY_3_DENIED;
            result += enrollmentsDenied[4] * COST_PRIORITY_4_DENIED;
            result += enrollmentsDenied[5] * COST_PRIORITY_5_DENIED;

            result += CountPairRelationUnsatisfied(solution) * COST_PAIR_UNSATISFIED;

            result += CountCreditInsufficient(solution) * COST_CREDIT_INSUFFICIENT;

            //for (int stuInd = 0; stuInd < solution.Count; ++stuInd)
            //{
            //    System.Diagnostics.Debug.WriteLine("Name: " + students[stuInd].name + "; Grade: " + students[stuInd].grade);
            //    for (int enrollInd = 0; enrollInd < solution[stuInd].Count; ++enrollInd)
            //    {
            //        Enrollment enroll = students[stuInd].enrollments[enrollInd];
            //        System.Diagnostics.Debug.WriteLine("ID: " + enroll.subjectId + "; " + "Priority: " + enroll.priority + "; Department ID: " + enroll.departmentId + "; Semester: " + enroll.semester);
            //        System.Diagnostics.Debug.WriteLine("Class Index: " + solution[stuInd][enrollInd]);
            //    }
            //}

            //System.Diagnostics.Debug.WriteLine("Conflict class pair: " + CountConflictClassPair(solution));
            //System.Diagnostics.Debug.WriteLine("Department bound unsatisfied: " + CountDepartmentBoundUnsatisfied(solution));
            //System.Diagnostics.Debug.WriteLine("Recommended room capacity exceeded: " + roomCapacityExceeded[0]);
            //System.Diagnostics.Debug.WriteLine("Maximum room capacity exceeded: " + roomCapacityExceeded[1]);
            //for (int priority = 0; priority <= 5; ++priority)
            //    System.Diagnostics.Debug.WriteLine("Priority " + priority + " enrollment denied: " + enrollmentsDenied[priority]);
            //System.Diagnostics.Debug.WriteLine("Pair relation unsatisfied: " + CountPairRelationUnsatisfied(solution));
            //System.Diagnostics.Debug.WriteLine("Credit insufficient: " + CountCreditInsufficient(solution));

            return result;
        }

        private List<List<int>> Clone(List<List<int>> list)
        {
            List<List<int>> result = new List<List<int>>();
            for (int i = 0; i < list.Count; ++i)
            {
                result.Add(new List<int>());
                foreach (int x in list[i])
                    result[i].Add(x);
            }

            return result;
        }

        private void Backtrack(int ordInd)
        {
            //if (iterationCount >= 100)
            //    return;
            //if (!(bestSolution is null))
            //    return;
            if (stopWatch.ElapsedMilliseconds > MAXIMUM_SEARCH_SECONDS * 1000)
                return;
            if (currentCost >= bestCost)
                return;

            if (ordInd == backtrackOrder.Count)
            {
                solutionFound = true;
                double cost = GetSolutionCost(currentSolution);
                //++iterationCount;
                System.Diagnostics.Debug.WriteLine("Current Cost: " + currentCost + "/" + cost);
                if (cost < bestCost)
                {
                    System.Diagnostics.Debug.WriteLine("Cost Updated");
                    bestSolution = Clone(currentSolution);
                    bestCost = cost;
                }
                return;
            }

            int stuInd = backtrackOrder[ordInd].stuInd;
            int enrollInd = backtrackOrder[ordInd].enrollInd;
            int subjectId = GetSubjectId(stuInd, enrollInd);
            int priority = students[stuInd].enrollments[enrollInd].priority;

            List<int> classIndices = new List<int>();
            for (int i = 0; i < classes[subjectId].Count; ++i)
                classIndices.Add(i);

            classIndices = classIndices.OrderByDescending(o =>
            {
                int roomId = classes[subjectId][o].roomId;
                int period = classes[subjectId][o].period;
                int maximumCapacity = rooms[roomId].maximumCapacity;

                bool[] daysOfWeek = classes[subjectId][o].daysOfWeek;
                bool[] semesters = GetSemesterAsBool(classes[subjectId][o].semester);

                int maximumCount = 0;
                for (int semester = 0; semester < 2; ++semester)
                    for (int day = 0; day < 5; ++day)
                        if (semesters[semester] && daysOfWeek[day])
                            maximumCount = Math.Max(maximumCount, studentCount[semester, day, period - 1][roomId]);
                return maximumCapacity - maximumCount;
            }).ToList();

            for (int i = 1; i <= classes[subjectId].Count; ++i)
            {
                int period = classes[subjectId][i - 1].period;
                int roomId = classes[subjectId][i - 1].roomId;
                bool[] daysOfWeek = classes[subjectId][i - 1].daysOfWeek;
                bool[] semesters = GetSemesterAsBool(classes[subjectId][i - 1].semester);

                int maximumCapacity = rooms[roomId].maximumCapacity;
                int maximumCount = 0;
                for (int semester = 0; semester < 2; ++semester)
                    for (int day = 0; day < 5; ++day)
                        if (semesters[semester] && daysOfWeek[day])
                            maximumCount = Math.Max(maximumCount, studentCount[semester, day, period - 1][roomId]);

                //Check for capacity
                if (maximumCount >= maximumCapacity)
                    continue;

                int classId = classes[subjectId][i - 1].id;

                //Check for conflict
                bool IsConflicted = false;
                for (int semester = 0; semester < 2; ++semester)
                {
                    for (int day = 0; day < 5; ++day)
                    {
                        if (!semesters[semester] || !daysOfWeek[day])
                            continue;
                        foreach (int id in schedules[stuInd, semester, day, period - 1])
                        {
                            if (!IsAcceptableConflict(classId, id))
                            {
                                IsConflicted = true;
                                break;
                            }
                        }
                    }
                }

                if (IsConflicted)
                    continue;

                currentSolution[stuInd][enrollInd] = i;

                //If the solution is completed for a student, check whether the schedule is valid for that student
                if (studentProgress[stuInd] == students[stuInd].enrollments.Count - 1 && !IsValidForStudent(stuInd, currentSolution[stuInd]))
                {
                    currentSolution[stuInd][enrollInd] = -1;
                    continue;
                }

                ++studentProgress[stuInd];

                int recommendedCapacity = rooms[roomId].recommendedCapacity;
                for (int semester = 0; semester < 2; ++semester)
                {
                    for (int day = 0; day < 5; ++day)
                    {
                        if (semesters[semester] && daysOfWeek[day])
                        {
                            if (studentCount[semester, day, period - 1][roomId] >= recommendedCapacity)
                                currentCost += COST_RECOMMENDED_CAPACITY_EXCEEDED;
                            ++studentCount[semester, day, period - 1][roomId];
                        }
                    }
                }

                for (int semester = 0; semester < 2; ++semester)
                    for (int day = 0; day < 5; ++day)
                        if (semesters[semester] && daysOfWeek[day])
                            schedules[stuInd, semester, day, period - 1].Add(classId);

                Backtrack(ordInd + 1);

                --studentProgress[stuInd];

                for (int semester = 0; semester < 2; ++semester)
                {
                    for (int day = 0; day < 5; ++day)
                    {
                        if (semesters[semester] && daysOfWeek[day])
                        {
                            --studentCount[semester, day, period - 1][roomId];
                            if (studentCount[semester, day, period - 1][roomId] >= recommendedCapacity)
                                currentCost -= COST_RECOMMENDED_CAPACITY_EXCEEDED;
                        }
                    }
                }

                for (int semester = 0; semester < 2; ++semester)
                    for (int day = 0; day < 5; ++day)
                        if (semesters[semester] && daysOfWeek[day])
                            schedules[stuInd, semester, day, period - 1].RemoveAt(schedules[stuInd, semester, day, period - 1].Count - 1);

                currentSolution[stuInd][enrollInd] = -1;
            }

            if (priority > 0)
            {
                currentSolution[stuInd][enrollInd] = 0;

                //If the solution is completed for a student, check whether the schedule is valid for that student
                if (studentProgress[stuInd] == students[stuInd].enrollments.Count - 1 && !IsValidForStudent(stuInd, currentSolution[stuInd]))
                {
                    currentSolution[stuInd][enrollInd] = -1;
                    return;
                }

                ++studentProgress[stuInd];
                currentCost += GetCostFromPriority(priority);

                Backtrack(ordInd + 1);

                currentCost -= GetCostFromPriority(priority);
                --studentProgress[stuInd];

                currentSolution[stuInd][enrollInd] = -1;
            }
        }

        private void SchedulingByBruteForce()
        {
            currentSolution = new List<List<int>>();
            backtrackOrder = new List<(int stuInd, int enrollInd)>();
            for (int stuInd = 0; stuInd < students.Count; ++stuInd)
            {
                currentSolution.Add(new List<int>());
                for (int enrollInd = 0; enrollInd < students[stuInd].enrollments.Count; ++enrollInd)
                {
                    backtrackOrder.Add((stuInd, enrollInd));
                    currentSolution[stuInd].Add(-1);
                }
            }
            backtrackOrder = backtrackOrder.OrderBy(o => (o.stuInd, students[o.stuInd].enrollments[o.enrollInd].priority, classes[GetSubjectId(o.stuInd, o.enrollInd)].Count)).ToList();

            studentCount = new Dictionary<int, int>[2, 5, 10];
            for (int semester = 0; semester < 2; ++semester)
            {
                for (int day = 0; day < 5; ++day)
                {
                    for (int period = 0; period < 10; ++period)
                    {
                        studentCount[semester, day, period] = new Dictionary<int, int>();
                        foreach (int item in rooms.Keys)
                        {
                            studentCount[semester, day, period].Add(item, 0);
                        }
                    }
                }
            }

            studentProgress = new int[students.Count];
            for (int i = 0; i < students.Count; ++i)
                studentProgress[i] = 0;

            schedules = new List<int>[students.Count, 2, 5, 10];
            for (int stuInd = 0; stuInd < students.Count; ++stuInd)
                for (int semester = 0; semester < 2; ++semester)
                    for (int day = 0; day < 5; ++day)
                        for (int period = 0; period < 10; ++period)
                            schedules[stuInd, semester, day, period] = new List<int>();

            bestSolution = null;
            solutionFound = false;
            bestCost = Double.MaxValue;
            currentCost = 0;
            //iterationCount = 0;

            for (int stuInd = 0; stuInd < students.Count; ++stuInd)
            {
                Debug.WriteLine("Student: " + students[stuInd].name);
                for (int enrollInd = 0; enrollInd < students[stuInd].enrollments.Count; ++enrollInd)
                {
                    int subjectId = GetSubjectId(stuInd, enrollInd);
                    Debug.WriteLine("Name: " + subjects[subjectId].name);

                    Debug.Write("Period:");
                    foreach (Class c in classes[subjectId])
                    {
                        Debug.Write(" " + c.period);
                    }
                    Debug.WriteLine("");
                }
            }

            stopWatch = new Stopwatch();
            stopWatch.Start();

            Backtrack(0);

            stopWatch.Stop();
        }

        private List<List<int>> ScheduleByGreedy()
        {
            List<(int stuInd, int enrollInd)> greedyOrder = new List<(int stuInd, int enrollInd)>();
            List<List<int>> solution = new List<List<int>>();
            for (int stuInd = 0; stuInd < students.Count; ++stuInd)
            {
                solution.Add(new List<int>());
                for (int enrollInd = 0; enrollInd < students[stuInd].enrollments.Count; ++enrollInd)
                {
                    greedyOrder.Add((stuInd, enrollInd));
                    solution[stuInd].Add(-1);
                }
            }

            studentCount = new Dictionary<int, int>[2, 5, 10];
            for (int semester = 0; semester < 2; ++semester)
            {
                for (int day = 0; day < 5; ++day)
                {
                    for (int period = 0; period < 10; ++period)
                    {
                        studentCount[semester, day, period] = new Dictionary<int, int>();
                        foreach (int item in rooms.Keys)
                        {
                            studentCount[semester, day, period].Add(item, 0);
                        }
                    }
                }
            }

            schedules = new List<int>[students.Count, 2, 5, 10];
            for (int stuInd = 0; stuInd < students.Count; ++stuInd)
                for (int semester = 0; semester < 2; ++semester)
                    for (int day = 0; day < 5; ++day)
                        for (int period = 0; period < 10; ++period)
                            schedules[stuInd, semester, day, period] = new List<int>();

            for (int orderInd = 0; orderInd < greedyOrder.Count; ++orderInd)
            {
                int stuInd = greedyOrder[orderInd].stuInd;
                int enrollInd = greedyOrder[orderInd].enrollInd;

                int bestClassInd = -1;
                int bestCost = int.MinValue;

                int subjectId = GetSubjectId(stuInd, enrollInd);
                for (int classInd = 0; classInd < classes[subjectId].Count; ++classInd)
                {
                    int roomId = classes[subjectId][classInd].roomId;
                    int period = classes[subjectId][classInd].period;
                    bool[] daysOfWeek = classes[subjectId][classInd].daysOfWeek;
                    bool[] semesters = GetSemesterAsBool(classes[subjectId][classInd].semester);
                    int classId = classes[subjectId][classInd].id;

                    //Check for conflict
                    bool IsConflicted = false;
                    for (int semester = 0; semester < 2; ++semester)
                    {
                        for (int day = 0; day < 5; ++day)
                        {
                            if (!semesters[semester] || !daysOfWeek[day])
                                continue;
                            foreach (int id in schedules[stuInd, semester, day, period - 1])
                            {
                                if (!IsAcceptableConflict(classId, id))
                                {
                                    IsConflicted = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (IsConflicted)
                        continue;

                    int maximumCapacity = rooms[roomId].maximumCapacity;
                    int maximumCount = 0;
                    for (int semester = 0; semester < 2; ++semester)
                        for (int day = 0; day < 5; ++day)
                            if (semesters[semester] && daysOfWeek[day])
                                maximumCount = Math.Max(maximumCount, studentCount[semester, day, period - 1][roomId]);

                    int cost = maximumCapacity - maximumCount;
                    if (cost > bestCost)
                    {
                        bestCost = cost;
                        bestClassInd = classInd;
                    }
                }

                solution[stuInd][enrollInd] = bestClassInd + 1;

                if (bestClassInd != -1)
                {
                    int roomId = classes[subjectId][bestClassInd].roomId;
                    int period = classes[subjectId][bestClassInd].period;
                    bool[] daysOfWeek = classes[subjectId][bestClassInd].daysOfWeek;
                    bool[] semesters = GetSemesterAsBool(classes[subjectId][bestClassInd].semester);
                    int classId = classes[subjectId][bestClassInd].id;

                    solution[stuInd][enrollInd] = bestClassInd;

                    for (int semester = 0; semester < 2; ++semester)
                    {
                        for (int day = 0; day < 5; ++day)
                        {
                            if (semesters[semester] && daysOfWeek[day])
                            {
                                ++studentCount[semester, day, period - 1][roomId];
                            }
                        }
                    }

                    for (int semester = 0; semester < 2; ++semester)
                        for (int day = 0; day < 5; ++day)
                            if (semesters[semester] && daysOfWeek[day])
                                schedules[stuInd, semester, day, period - 1].Add(classId);
                }
            }

            return solution;
        }

        private List<List<List<int>>> getNeighbors(List<List<int>> solution)
        {
            List<List<List<int>>> neighbors = new List<List<List<int>>>();
            //for(int stuInd = 0; )

            return neighbors;
        }

        private List<List<int>> ScheduleByTabuSearch()
        {
            int iterationLimit = 10;

            List<List<int>> s0 = ScheduleByGreedy();
            List<List<int>> sBest = s0;
            List<List<int>> bestCandidate = s0;
            double bestCost = GetSolutionCost(s0);
            double candidateCost = GetSolutionCost(s0);

            List<(int stuInd, int enrollInd, int classInd)> tabuList1 = new List<(int stuInd, int enrollInd, int classInd)>();
            List<(int stuInd, int enrollInd1, int enrollInd2)> tabuList2 = new List<(int stuInd, int enrollInd1, int enrollInd2)>();

            int iteration = 0;
            while (iteration < iterationLimit)
            {
                List<List<List<int>>> neighbors = new List<List<List<int>>>();

                int neighborType = -1;
                (int stuInd, int enrollInd, int classInd) tabuType1 = (-1, -1, -1);
                (int stuInd, int enrollInd1, int enrollInd2) tabuType2 = (-1, -1, -1);

                //Get neighbor of type 1
                for (int stuInd = 0; stuInd < students.Count; ++stuInd)
                {
                    for (int enrollInd = 0; enrollInd < students[stuInd].enrollments.Count; ++enrollInd)
                    {
                        int subjectId = GetSubjectId(stuInd, enrollInd);
                        for (int classInd = 0; classInd <= classes[subjectId].Count; ++classInd)
                        {
                            (int stuInd, int enrollInd, int classInd) tabu = (stuInd, enrollInd, classInd);
                            if (tabuList1.Contains(tabu))
                                continue;

                            List<List<int>> neighbor = Clone(bestCandidate);
                            neighbor[stuInd][enrollInd] = classInd;
                            neighbors.Add(neighbor);
                        }
                    }
                }

                //Get neighbor of type 2
                //for (int stuInd = 0; stuInd < students.Count; ++stuInd)
                //{
                //    for (int enrollInd = 0; enrollInd < students[stuInd].enrollments.Count; ++enrollInd)
                //    {
                //        int subjectId = GetSubjectId(stuInd, enrollInd);
                //        for (int classInd = 0; classInd <= classes[subjectId].Count; ++classInd)
                //        {
                //            (int stuInd, int enrollInd, int classInd) tabu = (stuInd, enrollInd, classInd);
                //            if (tabuList1.Contains(tabu))
                //                continue;

                //            tabuList1.Add(tabu);

                //            List<List<int>> neighbor = Clone(bestCandidate);
                //            neighbor[stuInd][enrollInd] = classInd;
                //            neighbors.Add(neighbor);
                //        }
                //    }
                //}

                foreach (List<List<int>> solution in neighbors)
                {
                    double solutionCost = GetSolutionCost(solution);
                    if (solutionCost > candidateCost)
                    {
                        bestCandidate = solution;
                        candidateCost = solutionCost;
                    }
                }


            }

            return sBest;
        }

        private void btnSchedule_Click(object sender, EventArgs e)
        {
            //List<int> tmp = new List<int>();
            //tmp.Add(1);
            //tmp.Add(1);
            //tmp.Add(3);
            //tmp.Add(1);
            //tmp.Add(1);
            //tmp.Add(1);
            //tmp.Add(0);
            //tmp.Add(0);
            //tmp.Add(4);
            //tmp.Add(4);
            //tmp.Add(3);
            //tmp.Add(2);

            //MessageBox.Show("Value: " + IsValidForStudent(0, tmp));

            const int stackSize = 0x400000;
            var thread = new Thread(SchedulingByBruteForce, stackSize);
            thread.Start();
            thread.Join();

            if (solutionFound == false)
            {
                MessageBox.Show("Unable to find an appropriate schedule for all student");
                return;
            }

            for (int stuInd = 0; stuInd < bestSolution.Count; ++stuInd)
            {
                System.Diagnostics.Debug.WriteLine("Name: " + students[stuInd].name + "; Grade: " + students[stuInd].grade);
                for (int enrollInd = 0; enrollInd < bestSolution[stuInd].Count; ++enrollInd)
                {
                    Enrollment enroll = students[stuInd].enrollments[enrollInd];
                    System.Diagnostics.Debug.WriteLine("ID: " + enroll.subjectId + "; " + "Priority: " + enroll.priority + "; Department ID: " + enroll.departmentId + "; Semester: " + enroll.semester);
                    System.Diagnostics.Debug.WriteLine("Class Index: " + bestSolution[stuInd][enrollInd]);
                }
            }
        }

        //input
        /************************************************************************************************************************/
        int departmentCnt = 0;
        int roomCnt = 0;
        int subjectCnt = 0;
        private int FindSubject(string value)
        {
            foreach (KeyValuePair<int, Subject> item in subjects)
            {
                if (item.Value.name == value) return (int)item.Key;
            }
            return -1;
        }

        private int FindDepartment(string value)
        {
            foreach (KeyValuePair<int, Department> item in departments)
            {
                if (item.Value.name == value) return (int)item.Key;
            }
            return -1;
        }

        private int FindRoom(string value)
        {
            foreach (KeyValuePair<int, Room> item in rooms)
            {
                if (item.Value.name == value) return (int)item.Key;
            }
            return -1;
        }

        private int FindClassID(int subId, int period)
        {
            List<Class> temp = new List<Class>();
            temp = classes[subId];
            foreach (Class cl in temp)
            {
                if (cl.period == period)
                {
                    return cl.id;
                }
            }
            return -1;
        }

        private string ClearSpace(string name)
        {
            int i = 0;
            while (name[i] == ' ')
            {
                i++;
            }
            name = name.Substring(i);
            i = 0;
            while (name[name.Length - 1 - i] == ' ')
            {
                i++;
            }
            if(i != 0)
                name = name.Remove(name.Length - i);
            return name;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //LoadDepartment("C:\\Users\\XuanQuang\\Dropbox\\DuAn_Quan_Quang\\Tai Lieu Quan\\department.xlsx");
            //LoadSubject("C:\\Users\\XuanQuang\\Dropbox\\DuAn_Quan_Quang\\Tai Lieu Quan\\subject_list.xlsx");
            //LoadRoom("C:\\Users\\XuanQuang\\Dropbox\\DuAn_Quan_Quang\\Tai Lieu Quan\\room.xlsx");

            //LoadMasterSchedule("C:\\Users\\XuanQuang\\Dropbox\\DuAn_Quan_Quang\\Tai Lieu Quan\\Master Schedule.xlsx");

            //LoadPairRela("C:\\Users\\XuanQuang\\Dropbox\\DuAn_Quan_Quang\\Tai Lieu Quan\\pair_relation.xlsx");
            //LoadConflict("C:\\Users\\XuanQuang\\Dropbox\\DuAn_Quan_Quang\\Tai Lieu Quan\\conflict.xlsx");
            //LoadCredit("C:\\Users\\XuanQuang\\Dropbox\\DuAn_Quan_Quang\\Tai Lieu Quan\\credit.xlsx");

            //students = new List<Student>();
            //LoadStudent("C:\\Users\\XuanQuang\\Dropbox\\DuAn_Quan_Quang\\Tai Lieu Quan\\Request cua HS\\Max Nguyen (smaller).xlsx");
        }

        private bool isEmpty(Object obj)
        {
            if (obj.Equals(DBNull.Value) || obj.ToString() == "")
            {
                return true;
            }
            return false;
        }

        //MasterSchedule

        private void btnMasterSchedule_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadMasterSchedule(ofd.FileName);
                }

            }
        }

        private void LoadMasterSchedule(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });
            reader.Close();

            System.Data.DataTable table = result.Tables[0];

            classes = new Dictionary<int, List<Class>>();
            int classCnt = 1;
            for (int col = 1; col < table.Columns.Count; col++)
            {
                for (int rowI = 2; rowI < table.Rows.Count; rowI += 3)
                {
                    string name, day, teacher, room; name = day = teacher = room = "";
                    int semester = 0;
                    if (!isEmpty(table.Rows[rowI][col]))
                    {
                        name = (string)table.Rows[rowI][col];
                        name = ClearSpace(name);
                        string t = name.Substring(name.Length - 4);
                        if (t == "(S1)")
                        {
                            name = name.Remove(name.Length - 4, 4);
                            semester = 1;
                        }
                        else if (t == "(S2)")
                        {
                            name = name.Remove(name.Length - 4, 4);
                            semester = 2;
                        }
                        else
                        {
                            semester = 3;
                        }
                        name = ClearSpace(name);
                        day = ClearSpace((string)table.Rows[rowI + 1][col]);
                        int temp = rowI;
                        while (table.Rows[temp][0].Equals(DBNull.Value) || (string)table.Rows[temp][0] == "")
                        {
                            temp -= 3;
                        }
                        teacher = ClearSpace((string)table.Rows[temp][0]);
                        room = ClearSpace((string)table.Rows[rowI + 2][col]);
                        Class cl = new Class
                        {
                            id = classCnt,
                            period = col,
                            roomId = FindRoom(room),
                            semester = semester,
                            subjectId = FindSubject(name),
                            teacherName = teacher,
                            daysOfWeek = new bool[5]
                        };
                        for (int i = 0; i < day.Length; i++)
                        {
                            char a = day[i];
                            if (a == 'M')
                            {
                                cl.daysOfWeek[0] = true;
                            }
                            else if (a == 'T')
                            {
                                cl.daysOfWeek[1] = true;
                            }
                            else if (a == 'W')
                            {
                                cl.daysOfWeek[2] = true;
                            }
                            else if (a == 'R')
                            {
                                cl.daysOfWeek[3] = true;
                            }
                            else
                            {
                                cl.daysOfWeek[4] = true;
                            }
                        }

                        if (!classes.ContainsKey(cl.subjectId))
                        {
                            classes.Add(cl.subjectId, new List<Class>());
                        }
                        classes[cl.subjectId].Add(cl);
                        //System.Diagnostics.Debug.WriteLine(name + "; " + day + "; " + teacher + "; " + room + "; " + semester + ";");
                        classCnt++;
                        //System.Diagnostics.Debug.WriteLine(name + " " + cl.subjectId);
                    }
                }
            }

            /*foreach (KeyValuePair<int, List<Class>> sub in classes)
            {
                System.Diagnostics.Debug.WriteLine(sub.Key);
                foreach (Class cl in sub.Value)
                {
                    System.Diagnostics.Debug.WriteLine(subjects[cl.subjectId].name + " " + cl.roomId + " " + cl.period);
                }
            }*/

            dtGridView.DataSource = result.Tables[0];
        }

        //Student Registration

        private void btnStudent_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true, Multiselect = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    students = new List<Student>();
                    foreach (string st in ofd.FileNames)
                    {
                        LoadStudent(st);
                    }
                }

            }
        }

        private void LoadStudent(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = false
                }
            });
            reader.Close();
            Student st = new Student();
            System.Data.DataTable table = result.Tables[0];
            st.name = ClearSpace((string)table.Rows[0][1]);
            st.grade = (int)(double)table.Rows[0][3];
            st.enrollments = new List<Enrollment>();
            for (int rowI = 2; rowI < table.Rows.Count; rowI++)
            {
                //int subject, department, priority, semester; subject = department = priority = semester = 0;
                bool flag = true;
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    if (isEmpty(table.Rows[rowI][col]))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    Enrollment en = new Enrollment();
                    en.subjectId = FindSubject(ClearSpace((string)table.Rows[rowI][0]));
                    en.priority = (int)(double)table.Rows[rowI][1];
                    en.departmentId = FindDepartment(ClearSpace((string)table.Rows[rowI][2]));
                    en.semester = (int)(double)table.Rows[rowI][3];
                    st.enrollments.Add(en);
                }
            }
            students.Add(st);
            /*foreach (Student stu in students)
            {
                System.Diagnostics.Debug.WriteLine(stu.grade + " " + stu.name);
                foreach (Enrollment en in stu.enrollments)
                {
                    System.Diagnostics.Debug.WriteLine(subjects[en.subjectId].name + " " + en.priority);
                }
            }*/
            dtGridView.DataSource = result.Tables[0];
        }

        //Department

        private void btnDepartment_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadDepartment(ofd.FileName);
                }

            }
        }

        private void LoadDepartment(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });
            reader.Close();
            System.Data.DataTable table = result.Tables[0];
            departments = new Dictionary<int, Department>();
            for (int rowI = 0; rowI < table.Rows.Count; rowI++)
            {
                bool flag = true;
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    if (isEmpty(table.Rows[rowI][col]))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    Department de = new Department();
                    string name = ClearSpace((string)table.Rows[rowI][0]);
                    de.minBound = (int)(double)table.Rows[rowI][1];
                    de.maxBound = (int)(double)table.Rows[rowI][2];
                    if (FindDepartment(name) == -1)
                    {
                        de.name = name;
                        de.id = departmentCnt;
                        departments.Add(de.id, de);
                        departmentCnt++;
                    }
                }
            }
            /*foreach (KeyValuePair<int, Department> sub in departments)
            {
                System.Diagnostics.Debug.WriteLine(sub.Key + " " + sub.Value.name);
            }*/
            dtGridView.DataSource = result.Tables[0];
        }

        //Subject

        private void btnSubject_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadSubject(ofd.FileName);
                }

            }
        }

        private void LoadSubject(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });
            reader.Close();
            System.Data.DataTable table = result.Tables[0];
            subjects = new Dictionary<int, Subject>();
            for (int rowI = 0; rowI < table.Rows.Count; rowI++)
            {
                if (!isEmpty(table.Rows[rowI][0]))
                {
                    Subject sub = new Subject();
                    sub.name = ClearSpace((string)table.Rows[rowI][0]);
                    if (FindSubject(sub.name) == -1)
                    {
                        sub.id = subjectCnt;
                        subjects.Add(sub.id, sub);
                        subjectCnt++;
                    }
                }
            }
            /*foreach (KeyValuePair<int, Subject> sub in subjects)
            {
                System.Diagnostics.Debug.WriteLine(sub.Key + " " + sub.Value.name);
            }*/
            dtGridView.DataSource = result.Tables[0];
        }

        //Room

        private void btnRoom_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadRoom(ofd.FileName);
                }

            }
        }

        private void LoadRoom(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });
            reader.Close();
            System.Data.DataTable table = result.Tables[0];
            rooms = new Dictionary<int, Room>();
            for (int rowI = 0; rowI < table.Rows.Count; rowI++)
            {
                bool flag = true;
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    var cell = table.Rows[rowI][col];
                    if (isEmpty(cell))
                    {
                        flag = false;
                        break;
                    }
                }
                //MessageBox.Show("Flag: " + flag);
                if (flag)
                {
                    Room rm = new Room
                    {
                        name = ClearSpace((string)table.Rows[rowI][0]),
                        recommendedCapacity = (int)(double)table.Rows[rowI][1],
                        maximumCapacity = (int)(double)table.Rows[rowI][2]
                    };
                    if (FindRoom(rm.name) == -1)
                    {
                        rm.id = roomCnt;
                        rooms.Add(rm.id, rm);
                        roomCnt++;
                    }
                }
            }
            //System.Diagnostics.Debug.WriteLine(rooms.Count);
            /*foreach (KeyValuePair<int, Room> sub in rooms)
            {
                System.Diagnostics.Debug.WriteLine(sub.Key + " " + sub.Value.name);
            }*/
            dtGridView.DataSource = result.Tables[0];
        }

        //PairRelation

        private void btnPairRela_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadPairRela(ofd.FileName);
                }

            }
        }

        private void LoadPairRela(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });
            reader.Close();
            System.Data.DataTable table = result.Tables[0];
            pairRelations = new List<PairRelation>();
            for (int rowI = 0; rowI < table.Rows.Count; rowI++)
            {
                bool flag = true;
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    if (isEmpty(table.Rows[rowI][col]))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    PairRelation pr = new PairRelation();
                    int subId1 = FindSubject(ClearSpace((string)table.Rows[rowI][0]));
                    int period1 = (int)(double)table.Rows[rowI][1];
                    int subId2 = FindSubject(ClearSpace((string)table.Rows[rowI][2]));
                    int period2 = (int)(double)table.Rows[rowI][3];
                    if (subId1 == -1)
                    {
                        MessageBox.Show("Invalid Class " + ClearSpace((string)table.Rows[rowI][0]));
                        break;
                    }
                    if (subId2 == -1)
                    {
                        MessageBox.Show("Invalid Class " + ClearSpace((string)table.Rows[rowI][2]));
                        break;
                    }
                    if (period1 > 9 || period1 < 1)
                    {
                        MessageBox.Show("Invalid Period " + (string)table.Rows[rowI][1]);
                        break;
                    }
                    if (period2 > 9 || period2 < 1)
                    {
                        MessageBox.Show("Invalid Period " + (string)table.Rows[rowI][3]);
                        break;
                    }
                    pr.classId1 = FindClassID(subId1, period1);
                    pr.classId2 = FindClassID(subId2, period2);
                    pairRelations.Add(pr);
                }
            }
            /*foreach (PairRelation pr in pairRelations)
            {
                System.Diagnostics.Debug.WriteLine(pr.classId1 + " " + pr.classId2);
            }*/
            dtGridView.DataSource = result.Tables[0];
        }

        //Conflict

        private void btnConflict_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadConflict(ofd.FileName);
                }

            }
        }

        private void LoadConflict(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });
            reader.Close();
            System.Data.DataTable table = result.Tables[0];
            acceptableConflictRelations = new List<AcceptableConflictRelation>();
            for (int rowI = 0; rowI < table.Rows.Count; rowI++)
            {
                bool flag = true;
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    if (isEmpty(table.Rows[rowI][col]))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    AcceptableConflictRelation con = new AcceptableConflictRelation();
                    int subId1 = (int)FindSubject(ClearSpace((string)table.Rows[rowI][1]));
                    int period = (int)(double)table.Rows[rowI][0];
                    int subId2 = (int)FindSubject(ClearSpace((string)table.Rows[rowI][2]));
                    if(subId1 == -1)
                    {
                        MessageBox.Show("Invalid Class " + ClearSpace((string)table.Rows[rowI][1]));
                        break;
                    }
                    if (subId2 == -1)
                    {
                        MessageBox.Show("Invalid Class " + ClearSpace((string)table.Rows[rowI][2]));
                        break;
                    }
                    if(period > 9 || period < 1)
                    {
                        MessageBox.Show("Invalid Period " + (string)table.Rows[rowI][0]);
                        break;
                    }
                    con.classId1 = FindClassID(subId1, period);
                    con.classId2 = FindClassID(subId2, period);
                    acceptableConflictRelations.Add(con);
                }
            }
            /*foreach (AcceptableConflictRelation con in acceptableConflictRelations)
            {
                System.Diagnostics.Debug.WriteLine(con.classId1 + " " + con.classId2);
            }*/
            dtGridView.DataSource = result.Tables[0];
        }

        //Credit

        private void btnCredit_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadCredit(ofd.FileName);
                }

            }
        }

        private void LoadCredit(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });
            reader.Close();
            System.Data.DataTable table = result.Tables[0];
            credits = new int[4];
            bool flag = true;
            for (int col = 1; col < table.Columns.Count; col++)
            {
                if (isEmpty(table.Rows[0][col]))
                {
                    flag = false;
                    break;
                }
            }
            if (flag)
            {
                for (int col = 1; col < table.Columns.Count; col++)
                {
                    credits[col - 1] = (int)(double)table.Rows[0][col];
                }
            }
            /*for (int i = 0; i < 4; i++)
            {
                System.Diagnostics.Debug.WriteLine(credits[i]);
            }*/
            dtGridView.DataSource = result.Tables[0];
        }

        //Export

        String DayOfWeek(bool[] daysOfWeek)
        {
            String day = "";
            if (daysOfWeek[0])
            {
                day += "M";
            }
            if (daysOfWeek[1])
            {
                day += "T";
            }
            if (daysOfWeek[2])
            {
                day += "W";
            }
            if (daysOfWeek[3])
            {
                day += "R";
            }
            if (daysOfWeek[4])
            {
                day += "F";
            }
            return day;
        }

        bool isEmptyPeriod(int period, Class[,] schedule)
        {
            for (int i = 0; i < 5; i++)
            {
                if (schedule[period, i] == null)
                {
                    return true;
                }
            }
            return false;
        }

        bool hasPeriod(int period, Class[,] schedule)
        {
            for (int i = 0; i < 5; i++)
            {
                if (schedule[period, i] != null)
                {
                    return true;
                }
            }
            return false;
        }

        bool isEmptyPeriodDay(int period, int day, Class[,] schedule)
        {
            if (schedule[period, day] == null)
                return true;
            return false;
        }

        private void ExportSchedules(string outputPath)
        {
            for (int i = 0; i < bestSolution.Count; i++)
            {
                var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Schedule");
                String stuGrade;
                if (students[i].grade == 9)
                {
                    stuGrade = "Freshman";
                }
                else if (students[i].grade == 10)
                {
                    stuGrade = "Sophomore";
                }
                else if (students[i].grade == 11)
                {
                    stuGrade = "Junior";
                }
                else
                {
                    stuGrade = "Senior";
                }
                ws.Cell(1, 1).Value = "Name";
                ws.Cell(1, 2).Value = students[i].name;
                ws.Cell(1, 3).Value = "Grade";
                ws.Cell(1, 4).Value = stuGrade;
                ws.Cell(2, 1).Value = "Class";
                ws.Cell(2, 2).Value = "Teacher Name";
                ws.Cell(2, 3).Value = "Room";
                ws.Cell(2, 4).Value = "Period";
                ws.Cell(2, 5).Value = "Days of The Week";
                ws.Cell(2, 6).Value = "Semester";
                int row = 3;
                for (int sem = 1; sem < 3; sem++)
                {
                    if (sem == 1)
                    {
                        ws.Cell(row, 1).Value = "First Semester";
                    }
                    else
                    {
                        ws.Cell(row, 1).Value = "Second Semester";
                    }
                    row++;

                    Class[,] schedule = new Class[15, 5];
                    for (int j = 0; j < bestSolution[i].Count; j++)
                    {
                        int classId = bestSolution[i][j] - 1;
                        if (classId == -1)
                        {
                            continue;
                        }
                        Class cl = classes[students[i].enrollments[j].subjectId][classId];
                        int period = cl.period;
                        if (cl.period == 10)
                        {
                            while (hasPeriod(period, schedule) && period < 15)
                            {
                                period++;
                            }
                        }
                        if (cl.semester == sem || cl.semester == 3)
                        {
                            bool[] dayOfWeek = cl.daysOfWeek;
                            for (int k = 0; k < 5; k++)
                            {
                                if (dayOfWeek[k])
                                {
                                    if (schedule[period, k] != null)
                                    {
                                        foreach (AcceptableConflictRelation con in acceptableConflictRelations)
                                        {
                                            if (con.classId1 == cl.id && con.classId2 == schedule[period, k].id)
                                            {
                                                schedule[period, k] = cl;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        schedule[period, k] = cl;
                                    }

                                }
                            }
                        }
                    }
                    for (int period = 1; period < 10; period++)
                    {
                        for (int day = 0; day < 5; day++)
                        {
                            if (schedule[period, day] == null)
                            {
                                int sub = FindSubject("Study Hall");
                                foreach (Class hall in classes[sub])
                                {
                                    if (hall.period == period && (hall.semester == 3 || hall.semester == sem))
                                    {
                                        schedule[period, day] = hall;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    for (int j = 1; j < 10; j++)
                    {
                        SortedList<int, Class> listOfClass = new SortedList<int, Class>();
                        int numberClass = 0;
                        bool[] dayOfWeek = new bool[5];
                        for (int day = 0; day < 5; day++)
                        {
                            if (!listOfClass.ContainsValue(schedule[j, day]))
                            {
                                listOfClass.Add(numberClass, schedule[j, day]);
                                numberClass++;
                            }
                            if (subjects[schedule[j, day].subjectId].name == "Study Hall")
                            {
                                dayOfWeek[day] = true;
                            }
                        }
                        for (int num = 0; num < numberClass; num++)
                        {
                            Class cl = listOfClass[num];
                            if (subjects[cl.subjectId].name == "Study Hall")
                            {
                                ws.Cell(row, 5).Value = DayOfWeek(dayOfWeek);
                            }
                            else
                            {
                                ws.Cell(row, 5).Value = DayOfWeek(cl.daysOfWeek);
                            }
                            ws.Cell(row, 1).Value = subjects[cl.subjectId].name;
                            ws.Cell(row, 2).Value = cl.teacherName;
                            ws.Cell(row, 3).Value = rooms[cl.roomId].name;
                            ws.Cell(row, 4).Value = j;
                            ws.Cell(row, 6).Value = sem;
                            row++;
                        }
                    }

                    for (int j = 10; j < 15; j++)
                    {
                        for (int day = 0; day < 5; day++)
                        {
                            if (schedule[j, day] != null)
                            {
                                Class cl = schedule[j, day];
                                ws.Cell(row, 1).Value = subjects[cl.subjectId].name;
                                ws.Cell(row, 2).Value = cl.teacherName;
                                ws.Cell(row, 3).Value = rooms[cl.roomId].name;
                                ws.Cell(row, 4).Value = 0;
                                ws.Cell(row, 5).Value = DayOfWeek(cl.daysOfWeek);
                                ws.Cell(row, 6).Value = sem;
                                row++;
                                break;
                            }
                        }

                    }
                    row++;
                }
                row++;
                ws.Columns(1, 6).AdjustToContents();
                ws.Cell(row, 1).Value = "Required Class Not Taken";
                int temp = 2;
                for (int j = 0; j < bestSolution[i].Count; j++)
                {
                    if(bestSolution[i][j] == 0 && students[i].enrollments[j].priority == 0)
                    {
                        ws.Cell(row, temp).Value = subjects[students[i].enrollments[j].subjectId].name;
                        temp++;
                    }
                }
                row++;
                ws.Cell(row, 1).Value = "Optional Class Not Taken";
                temp = 2;
                for (int j = 0; j < bestSolution[i].Count; j++)
                {
                    if (bestSolution[i][j] == 0 && students[i].enrollments[j].priority != 0)
                    {
                        ws.Cell(row, temp).Value = subjects[students[i].enrollments[j].subjectId].name;
                        temp++;
                    }
                }
                wb.SaveAs(outputPath + @"\Output_" + (String)students[i].name + "_" + stuGrade + ".xlsx");
            }
            MessageBox.Show("Completed");
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    SchedulingByBruteForce();

                    if (solutionFound == false)
                    {
                        MessageBox.Show("Unable to find an appropriate schedule for all student");
                        return;
                    }

                    ExportSchedules(fbd.SelectedPath);
                }
            }
        }

        //Select File For Checker

        private void btnCheckerFileSelect_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true, Multiselect = true })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    studentsError = new List<Student>();
                    foreach (string st in ofd.FileNames)
                    {
                        LoadCheckerFileSelect(st);
                    }
                }
            }
        }

        private void LoadCheckerFileSelect(string filePath)
        {

            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = false
                }
            });
            reader.Close();
            Student st = new Student();
            System.Data.DataTable table = result.Tables[0];
            st.name = ClearSpace((string)table.Rows[0][1]);
            st.grade = (int)(double)table.Rows[0][3];
            st.enrollments = new List<Enrollment>();
            for (int rowI = 2; rowI < table.Rows.Count; rowI++)
            {
                //int subject, department, priority, semester; subject = department = priority = semester = 0;
                bool flag = true;
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    if (isEmpty(table.Rows[rowI][col]))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    Enrollment en = new Enrollment();
                    en.subjectId = FindSubject(ClearSpace((string)table.Rows[rowI][0]));
                    en.priority = (int)(double)table.Rows[rowI][1];
                    en.departmentId = FindDepartment(ClearSpace((string)table.Rows[rowI][2]));
                    en.semester = (int)(double)table.Rows[rowI][3];
                    st.enrollments.Add(en);
                }
            }
            studentsError.Add(st);
            /*foreach (Student stu in students)
            {
                System.Diagnostics.Debug.WriteLine(stu.grade + " " + stu.name);
                foreach (Enrollment en in stu.enrollments)
                {
                    System.Diagnostics.Debug.WriteLine(subjects[en.subjectId].name + " " + en.priority);
                }
            }*/
            dtGridView.DataSource = result.Tables[0];
        }

        //Checker for Students, grade, department

        private void ExportError(string outputPath)
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Schedule");
            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Grade";
            ws.Cell(1, 3).Value = "Request Error";
            int row = 2;
            for (int i = 0; i < studentsError.Count; i++)
            {
                int column = 1;
                ws.Cell(row, column).Value = (string) studentsError[i].name; column++;
                ws.Cell(row, column).Value = (int) studentsError[i].grade; column++;
                if (studentsError[i].grade < 9 || studentsError[i].grade > 12)
                {
                    ws.Cell(row, column).Value = "Unknown Grade"; column++;
                }
                int cntEn = 1;
                foreach(Enrollment en in studentsError[i].enrollments)
                { 
                    if(en.subjectId == -1)
                    {
                        ws.Cell(row, column).Value = "Unknown Subject Requested Number " + cntEn.ToString(); column++;
                    }
                    if (en.departmentId == -1)
                    {
                        ws.Cell(row, column).Value = "Unknown Department Requested Number " + cntEn.ToString(); column++;
                    }
                    if(en.semester > 3 || en.semester < 1)
                    {
                        ws.Cell(row, column).Value = "Unknown Semester Number " + cntEn.ToString(); column++;
                    }
                    cntEn++;
                }
                row++;
            }
            ws.Columns(1, 30).AdjustToContents();
            wb.SaveAs(outputPath + @"\Output_Error.xlsx");
        }

        private void btnChecker_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    ExportError(fbd.SelectedPath);
                    MessageBox.Show("completed");
                }
            }
        }

        //SelectMasterSchedule

        private void btnSelectMasterSchedule_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workdbook|*.xlsx", ValidateNames = true, Multiselect = false })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadSelectMasterSchedule(ofd.FileName);
                }
            }
        }

        private void LoadSelectMasterSchedule(string filePath)
        {
            FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read);
            IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(fs);
            DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });
            reader.Close();
            masterScheduleError = new List<Tuple<int, int>>();
            System.Data.DataTable table = result.Tables[0];
            for (int col = 1; col < table.Columns.Count; col++)
            {
                for (int rowI = 2; rowI < table.Rows.Count; rowI += 3)
                {
                    string name, day, teacher, room; name = day = teacher = room = "";
                    if (!isEmpty(table.Rows[rowI][col]))
                    {
                        name = ClearSpace((string)table.Rows[rowI][col]);
                        string t = name.Substring(name.Length - 4);
                        if (t == "(S1)" || t == "(S2)")
                        {
                            name = name.Remove(name.Length - 4, 4);
                            name = ClearSpace(name);
                        }
                        day = ClearSpace((string)table.Rows[rowI + 1][col]);
                        room = ClearSpace((string)table.Rows[rowI + 2][col]);
                        if(FindRoom(room) == -1)
                        {
                            var address = Tuple.Create(rowI + 2 + 2, col); // +2 to row because the first row is used for title and row counts from 0
                            masterScheduleError.Add(address);
                        }
                        if (FindSubject(name) == -1)
                        {
                            var address = Tuple.Create(rowI + 2, col);
                            masterScheduleError.Add(address);
                        }
                        
                        for (int i = 0; i < day.Length; i++)
                        {
                            char a = day[i];
                            if (!(a == 'M' || a == 'T' || a == 'W' || a == 'R' || a == 'F'))
                            {
                                var address = Tuple.Create(rowI + 1 + 2, col);
                                masterScheduleError.Add(address);
                                break;
                            }
                        }
                    }
                }
            }
            dtGridView.DataSource = result.Tables[0];
        }

        //Checker for Master Schedule Room and Subject

        private void ExportMSError(string outputPath)
        {
            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Schedule");
            ws.Cell(1, 1).Value = "Column";
            ws.Cell(1, 2).Value = "Row";
            int row = 2;
            foreach(Tuple<int, int> tp in masterScheduleError)
            {
                ws.Cell(row, 1).Value = (char)('A' + tp.Item2);
                ws.Cell(row, 2).Value = tp.Item1;
                row++;
            }
            ws.Columns(1, 2).AdjustToContents();
            wb.SaveAs(outputPath + @"\Output_MasterScheduleError.xlsx");
        }

        private void btnCheckMasterSchedule_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    ExportMSError(fbd.SelectedPath);
                    MessageBox.Show("completed");
                }
            }
        }
    }
}



// wb.SaveAs(@"C:\Users\XuanQuang\Dropbox\DuAn_Quan_Quang\Tai Lieu Quan\Schedule Output\Output_" + (String)students[i].name + "_" + stuGrade);
