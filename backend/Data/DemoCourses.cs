using Microsoft.EntityFrameworkCore;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Data
{
    // Extra demo courses, so the platform looks full.
    //
    // They are added:
    //   - automatically when a new, empty database is created (see DbSeeder), and
    //   - when an admin clicks "Add demo courses" in the admin panel.
    //
    // A course is only added if no course with the same title exists yet,
    // so running this twice does not create copies.
    //
    // Images and videos come from Cloudinary's public "demo" account,
    // so they do not use any space in your own Cloudinary account.
    public static class DemoCourses
    {
        private const string Images = "https://res.cloudinary.com/demo/image/upload/c_fill,w_750,h_422/";
        private const string Videos = "https://res.cloudinary.com/demo/video/upload/";

        // Sample videos and their length in seconds.
        private const string Dog = Videos + "dog.mp4";                          // 13 s
        private const string Elephants = Videos + "elephants.mp4";              // 52 s
        private const string Turtle = Videos + "sea_turtle.mp4";                // 15 s
        private const string SampleVideo = Videos + "samples/cld-sample-video.mp4"; // 12 s
        private const string Dance = Videos + "samples/dance-2.mp4";            // 20 s

        // Adds the demo courses that are missing. Returns how many were added.
        public static int AddMissing(AppDbContext db)
        {
            // The courses belong to the demo instructor, or else to the first instructor found.
            User instructor = db.Users.FirstOrDefault(u => u.Email == "instructor@smartlearn.dev");
            if (instructor == null)
            {
                instructor = db.Users.OrderBy(u => u.Id).FirstOrDefault(u => u.Role == Roles.Instructor);
            }
            if (instructor == null)
            {
                return 0; // nobody can own the courses
            }

            List<Course> courses = BuildCourses();
            int added = 0;
            DateTime now = DateTime.UtcNow;

            foreach (Course course in courses)
            {
                bool exists = db.Courses.Any(c => c.Title == course.Title);
                if (exists)
                {
                    continue;
                }

                course.InstructorId = instructor.Id;
                course.Category = FindOrCreateCategory(db, course.Category.Name);
                course.IsPublished = true;
                course.PublishedAt = now;
                course.CreatedAt = now;
                course.UpdatedAt = now;

                db.Courses.Add(course);
                added++;
            }

            db.SaveChanges();

            // Demo quizzes and coupon codes (also only the missing ones).
            AddMissingQuizzes(db);
            AddMissingCoupons(db, instructor);

            return added;
        }

        // ---------------------------------------------------------------
        // Demo quizzes: added to a section of a demo course when that section has no quiz yet.
        // ---------------------------------------------------------------
        private static void AddMissingQuizzes(AppDbContext db)
        {
            AddQuiz(db, "C# for Complete Beginners", "C# Basics", "C# Basics Quiz",
                NewQuestion("Which type stores a whole number like 42?", "string", "int", "bool", "double", 2,
                    "int is the type for whole numbers. double is for numbers with decimals."),
                NewQuestion("What does an if statement do?", "Repeats code", "Runs code only when a condition is true",
                    "Stores a value", "Ends the program", 2,
                    "if checks a condition and runs its block only when the condition is true."),
                NewQuestion("Which loop is best when you know how many times to repeat?", "for", "if", "switch", "return", 1,
                    "A for loop has a counter, so it is ideal for a known number of repetitions."));

            AddQuiz(db, "React - The Practical Guide", "Components and Props", "Components Quiz",
                NewQuestion("What is a React component?", "A CSS file", "A function that returns what to show on screen",
                    "A database table", "A browser plugin", 2,
                    "Components are JavaScript functions that return JSX."),
                NewQuestion("How does a parent pass data to a child component?", "With props", "With CSS",
                    "With localStorage", "It cannot", 1,
                    "Props are the inputs of a component, set by its parent."),
                NewQuestion("Can a child component change its props?", "Yes, anytime", "Only on Mondays",
                    "No, props are read-only", "Only with CSS", 3,
                    "Props are read-only. Use state for values that change."));

            AddQuiz(db, "Digital Marketing Essentials", "Marketing Foundations", "Marketing Basics Quiz",
                NewQuestion("What should you define before starting any campaign?", "The logo colour",
                    "Your target customer and goal", "The office address", "The number of employees", 2,
                    "Knowing who you talk to and what you want to achieve guides every other decision."),
                NewQuestion("Which goal is measurable?", "Be more popular", "Get 200 newsletter sign-ups this month",
                    "Look professional", "Post more", 2,
                    "A good goal has a number and a deadline."));

            AddQuiz(db, "Smartphone Photography Made Easy", "Composition and Light", "Composition Quiz",
                NewQuestion("What is the rule of thirds?", "Take three photos of everything",
                    "Place the subject along imaginary lines that split the frame in thirds",
                    "Use only three colours", "Edit for three minutes", 2,
                    "Placing the subject on the third lines makes photos feel balanced and natural."),
                NewQuestion("When is the 'golden hour'?", "Midday", "Just after sunrise and just before sunset",
                    "Midnight", "Any time it rains", 2,
                    "The low sun gives soft, warm light that flatters almost any subject."));

            db.SaveChanges();
        }

        private static void AddQuiz(AppDbContext db, string courseTitle, string sectionTitle, string quizTitle,
            params QuizQuestion[] questions)
        {
            Section section = db.Sections
                .Include(s => s.Quiz)
                .FirstOrDefault(s => s.Title == sectionTitle && s.Course.Title == courseTitle);

            if (section == null || section.Quiz != null)
            {
                return; // the course is missing, or the section already has a quiz
            }

            Quiz quiz = new Quiz { SectionId = section.Id, Title = quizTitle, PassPercent = 70 };
            for (int i = 0; i < questions.Length; i++)
            {
                questions[i].OrderIndex = i + 1;
                quiz.Questions.Add(questions[i]);
            }
            db.Quizzes.Add(quiz);
        }

        private static QuizQuestion NewQuestion(string text, string option1, string option2, string option3, string option4,
            int correctOption, string explanation)
        {
            return new QuizQuestion
            {
                Text = text,
                Option1 = option1,
                Option2 = option2,
                Option3 = option3,
                Option4 = option4,
                CorrectOption = correctOption,
                Explanation = explanation
            };
        }

        // ---------------------------------------------------------------
        // Demo coupon codes
        // ---------------------------------------------------------------
        private static void AddMissingCoupons(AppDbContext db, User instructor)
        {
            // Site-wide coupons belong to an admin (or to the instructor if there is no admin).
            User owner = db.Users.OrderBy(u => u.Id).FirstOrDefault(u => u.Role == Roles.Admin);
            if (owner == null)
            {
                owner = instructor;
            }

            if (!db.Coupons.Any(c => c.Code == "WELCOME20"))
            {
                db.Coupons.Add(new Coupon
                {
                    Code = "WELCOME20",
                    DiscountPercent = 20,
                    ForPlans = false,
                    CreatedById = owner.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!db.Coupons.Any(c => c.Code == "SUBSCRIBE10"))
            {
                db.Coupons.Add(new Coupon
                {
                    Code = "SUBSCRIBE10",
                    DiscountPercent = 10,
                    ForPlans = true,
                    CreatedById = owner.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            db.SaveChanges();
        }

        private static Category FindOrCreateCategory(AppDbContext db, string name)
        {
            // Look in the database first, then in categories added a moment ago (not saved yet).
            Category category = db.Categories.FirstOrDefault(c => c.Name == name);
            if (category == null)
            {
                category = db.Categories.Local.FirstOrDefault(c => c.Name == name);
            }
            if (category == null)
            {
                category = new Category { Name = name };
                db.Categories.Add(category);
            }
            return category;
        }

        // ---------------------------------------------------------------
        // The courses
        // ---------------------------------------------------------------
        private static List<Course> BuildCourses()
        {
            List<Course> courses = new List<Course>();

            // 1. Marketing
            Course marketing = NewCourse(
                "Digital Marketing Essentials",
                "Grow any business online with social media, SEO and email marketing",
                "Marketing", "Beginner", 999,
                "samples/ecommerce/accessories-bag.jpg", SampleVideo,
                "Learn how businesses find customers online.\n\nThis course walks you through the most important digital marketing channels: social media, search engines, email and paid ads. Every section ends with a small task you can apply to your own business or project.",
                "Create a simple marketing plan\nWrite posts that people want to share\nUnderstand the basics of SEO\nBuild and grow an email list\nMeasure results with simple numbers",
                "No marketing experience needed\nA social media account is helpful");
            marketing.Sections.Add(NewSection(1, "Marketing Foundations",
                NewLecture(1, "What is digital marketing?", SampleVideo, 12, true),
                NewLecture(2, "Know your customer", Dog, 13, false),
                NewLecture(3, "Setting goals you can measure", Turtle, 15, false)));
            marketing.Sections.Add(NewSection(2, "Social Media and Content",
                NewLecture(1, "Choosing the right platforms", Elephants, 52, false),
                NewLecture(2, "Writing posts that get attention", Dog, 13, false)));
            marketing.Sections.Add(NewSection(3, "SEO and Email",
                NewLecture(1, "How search engines rank pages", Turtle, 15, false),
                NewLecture(2, "Your first email campaign", SampleVideo, 12, false)));
            courses.Add(marketing);

            // 2. Business
            Course leadership = NewCourse(
                "Leadership and Teamwork Skills",
                "Lead people with confidence, run better meetings and handle conflict",
                "Business", "All Levels", 1499,
                "samples/imagecon-group.jpg", Elephants,
                "Great teams do not happen by accident.\n\nIn this course you will learn practical habits used by good leaders: giving clear goals, running short and useful meetings, giving feedback, and solving conflicts before they grow.",
                "Communicate goals clearly\nRun meetings that end on time\nGive and receive helpful feedback\nHandle disagreements calmly",
                "No experience needed\nUseful for new team leads and students working in groups");
            leadership.Sections.Add(NewSection(1, "Being a Leader",
                NewLecture(1, "What good leaders do differently", Elephants, 52, true),
                NewLecture(2, "Setting clear goals", Dog, 13, false)));
            leadership.Sections.Add(NewSection(2, "Working as a Team",
                NewLecture(1, "Running effective meetings", Turtle, 15, false),
                NewLecture(2, "Giving feedback that helps", SampleVideo, 12, false),
                NewLecture(3, "Handling conflict", Dog, 13, false)));
            courses.Add(leadership);

            // 3. Photography (free)
            Course photography = NewCourse(
                "Smartphone Photography Made Easy",
                "Take beautiful photos with the phone already in your pocket",
                "Photography & Video", "Beginner", 0,
                "samples/landscapes/nature-mountains.jpg", Turtle,
                "You do not need an expensive camera to take great pictures.\n\nLearn composition, light and simple editing, and practice with short exercises you can do anywhere.",
                "Use the rule of thirds\nFind and use good light\nTake better portraits and landscapes\nEdit photos in a few taps",
                "Any smartphone with a camera");
            photography.Sections.Add(NewSection(1, "Camera Basics",
                NewLecture(1, "Welcome and course overview", Turtle, 15, true),
                NewLecture(2, "Focus and exposure on your phone", Dog, 13, false)));
            photography.Sections.Add(NewSection(2, "Composition and Light",
                NewLecture(1, "The rule of thirds", Elephants, 52, false),
                NewLecture(2, "Shooting in golden hour", Turtle, 15, false)));
            photography.Sections.Add(NewSection(3, "Editing",
                NewLecture(1, "Quick edits that make a big difference", SampleVideo, 12, false)));
            courses.Add(photography);

            // 4. Music
            Course jazz = NewCourse(
                "Jazz Improvisation for Beginners",
                "Start playing your own jazz solos on any instrument",
                "Music", "Beginner", 1199,
                "samples/people/jazz.jpg", Dance,
                "Improvisation is the heart of jazz.\n\nThis course explains scales, chords and rhythm in simple words, then shows you how to turn them into short solos. Play along with the examples on any instrument.",
                "Understand the blues scale\nPlay over simple chord progressions\nFeel swing rhythm\nBuild your first short solos",
                "Know the basic notes on your instrument");
            jazz.Sections.Add(NewSection(1, "Jazz Basics",
                NewLecture(1, "What makes music sound like jazz?", Dance, 20, true),
                NewLecture(2, "Swing rhythm", Dog, 13, false)));
            jazz.Sections.Add(NewSection(2, "Scales and Chords",
                NewLecture(1, "The blues scale", Dance, 20, false),
                NewLecture(2, "The 2-5-1 progression", Turtle, 15, false)));
            jazz.Sections.Add(NewSection(3, "Your First Solos",
                NewLecture(1, "Call and response", Dance, 20, false),
                NewLecture(2, "Putting it all together", Elephants, 52, false)));
            courses.Add(jazz);

            // 5. Personal development
            Course cooking = NewCourse(
                "Cooking Basics: Cook Like a Chef at Home",
                "Knife skills, simple recipes and kitchen habits of professional chefs",
                "Personal Development", "All Levels", 799,
                "samples/people/kitchen-bar.jpg", Dog,
                "Cooking is a life skill that saves money and makes you healthier.\n\nA professional chef shows you how to hold a knife, plan your cooking, and make simple meals and desserts step by step.",
                "Use a kitchen knife safely\nPlan and prepare meals faster\nCook simple, healthy dishes\nMake an easy dessert",
                "A basic kitchen at home");
            cooking.Sections.Add(NewSection(1, "In the Kitchen",
                NewLecture(1, "Tools every home cook needs", Dog, 13, true),
                NewLecture(2, "Knife skills", SampleVideo, 12, false)));
            cooking.Sections.Add(NewSection(2, "Simple Recipes",
                NewLecture(1, "A 15-minute pasta", Turtle, 15, false),
                NewLecture(2, "Fresh salads", Dog, 13, false),
                NewLecture(3, "An easy dessert", Elephants, 52, false)));
            courses.Add(cooking);

            return courses;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private static Course NewCourse(string title, string subtitle, string categoryName, string level, decimal price,
            string image, string promoVideo, string description, string whatYouWillLearn, string requirements)
        {
            return new Course
            {
                Title = title,
                Subtitle = subtitle,
                // Only the name is set here; AddMissing swaps it for the real category.
                Category = new Category { Name = categoryName },
                Level = level,
                Language = "English",
                Price = price,
                ThumbnailUrl = Images + image,
                PromoVideoUrl = promoVideo,
                Description = description,
                WhatYouWillLearn = whatYouWillLearn,
                Requirements = requirements
            };
        }

        private static Section NewSection(int order, string title, params Lecture[] lectures)
        {
            Section section = new Section { Title = title, OrderIndex = order };
            section.Lectures.AddRange(lectures);
            return section;
        }

        private static Lecture NewLecture(int order, string title, string videoUrl, int seconds, bool isFreePreview)
        {
            return new Lecture
            {
                Title = title,
                Description = "In this lecture: " + title.ToLower().TrimEnd('?') + ".",
                VideoUrl = videoUrl,
                DurationSeconds = seconds,
                IsFreePreview = isFreePreview,
                OrderIndex = order
            };
        }
    }
}
