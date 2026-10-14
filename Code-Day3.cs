using System.Runtime.CompilerServices;

namespace day3InClass;

class Course
{
    public Dictionary<int, int> course_availseats = new();
    public int registeredStudents = 0;
    public readonly object seatLock = new();   // guards course_availseats across threads
    public readonly object studentCountLock = new(); // guards registeredStudents across threads

    public Course(int courses = 100, int availseats = 10_000)
    {
        for (int i = 1; i <= courses; i++)
        {
            course_availseats[i] = availseats;
        }
    }
}

class Student
{
    public int id;
    public List<int> courses;

    public Student(int in_id)
    {
        id = in_id;
        courses = new();
    }

    public void SelectCoursesForRegistration(int num)
    {
        while (courses.Count < num)
        {
            int pick = Random.Shared.Next(100) + 1; // 1-100
            if (!courses.Contains(pick))            // don't pick the same course twice
                courses.Add(pick);
        }
    }

    public bool RegisterMyCourses()
    {
        bool allRegistered = true;
        lock (Program.c.seatLock)
        {
            Thread.Sleep(100);

            foreach (var c in courses)
            {
                if (Program.c.course_availseats[c] > 0)
                    Program.c.course_availseats[c]--;
                else
                    allRegistered = false;          // course is full
            }

            lock (Program.c.studentCountLock)
            {
                Program.c.registeredStudents++;
            }
        }
        return allRegistered;
    }

    public void CountNumberOfStudents()
    {
        lock (Program.c.studentCountLock)
        {
            Thread.Sleep(100);
            
            int count = Program.c.registeredStudents;

            lock (Program.c.seatLock)
            {
                int seats = Program.c.course_availseats[1];
                Console.WriteLine($"Students: {count}, seats: {seats}");
            }
        }
    }


}

class Program
{
    public static Course c = new();

    static void Main(string[] args)
    {
        List<Thread> myThreads = new();
        List<Thread> myCountingThreads = new();
        for (int i = 1; i < 200; i++)
        {
            // 1) Create a thread to do N (1000) student registrations
            myThreads.Add(new Thread(() =>
            {
                for (int i = 1; i <= 1000; i++)
                {
                    Student s = new Student(i);          // create student
                    s.SelectCoursesForRegistration(5);   // Student.SelectCoursesForRegistration
                    if (s.RegisterMyCourses() == false)               // Student.RegisterMyCourses
                        Console.WriteLine($"Student {s.id} failed to register course list");
                }
            }));
        }

        for (int i = 1; i < 200; i++)
        {
            // Creats a thread to count how many students registered
            myCountingThreads.Add(new Thread(() =>
            {

                for (int i = 1; i <= 1000; i++)
                {
                    Student s = new Student(i);
                    s.CountNumberOfStudents();
                }
            }));
        }

        foreach (var x in myThreads)
        {
            x.Start();
        }

        foreach (var x in myCountingThreads)
        {
            x.Start();
        }

        foreach (var x in myThreads)
        {
            x.Join();
        }

        foreach (var x in myCountingThreads)
        {
            x.Join();
        }

        Console.WriteLine("All seats assigned");

        foreach (var x in c.course_availseats)
        {
            Console.WriteLine($"Course {x.Key} has {x.Value} seats still available");
        }
    }
}