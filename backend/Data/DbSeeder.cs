using SmartLearning.Api.Models;

namespace SmartLearning.Api.Data
{
    // Fills an EMPTY database with demo data, so the app is usable right away.
    // It does nothing if there is already at least one user.
    public static class DbSeeder
    {
        // Public sample files from Cloudinary's own "demo" account.
        // Your own uploads go to YOUR Cloudinary account (see appsettings.json).
        private const string DemoVideoDog = "https://res.cloudinary.com/demo/video/upload/dog.mp4";
        private const string DemoVideoElephants = "https://res.cloudinary.com/demo/video/upload/elephants.mp4";
        private const string DemoVideoTurtle = "https://res.cloudinary.com/demo/video/upload/sea_turtle.mp4";

        // demoPassword = password of the three demo accounts. It comes from
        // appsettings.json ("DemoPassword"). On a live server, set it to a secret value.
        public static void Seed(AppDbContext db, string demoPassword)
        {
            if (db.Users.Any())
            {
                return;
            }

            DateTime now = DateTime.UtcNow;

            // ---------- Users ----------
            User admin = new User
            {
                FullName = "Admin User",
                Email = "admin@smartlearn.dev",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword),
                Role = Roles.Admin,
                IsEmailVerified = true,
                CreatedAt = now
            };

            User instructor = new User
            {
                FullName = "Grace Hopper",
                Email = "instructor@smartlearn.dev",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword),
                Role = Roles.Instructor,
                IsEmailVerified = true,
                Headline = "Senior Software Engineer and Teacher",
                Bio = "I have been building software for 15 years and love teaching beginners.",
                CreatedAt = now
            };

            User student = new User
            {
                FullName = "Sam Student",
                Email = "student@smartlearn.dev",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPassword),
                Role = Roles.Student,
                IsEmailVerified = true,
                CreatedAt = now
            };

            db.Users.AddRange(admin, instructor, student);

            // ---------- Categories ----------
            Category development = new Category { Name = "Development" };
            Category business = new Category { Name = "Business" };
            Category design = new Category { Name = "Design" };
            Category marketing = new Category { Name = "Marketing" };
            Category itSoftware = new Category { Name = "IT & Software" };
            Category personal = new Category { Name = "Personal Development" };
            Category photography = new Category { Name = "Photography & Video" };
            Category music = new Category { Name = "Music" };

            db.Categories.AddRange(development, business, design, marketing, itSoftware, personal, photography, music);

            // ---------- Subscription plans ----------
            db.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    Name = "Monthly",
                    Description = "Access every course for 1 month.",
                    Price = 499,
                    DurationDays = 30,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Name = "Quarterly",
                    Description = "Access every course for 3 months. Save 13%.",
                    Price = 1299,
                    DurationDays = 90,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Name = "Yearly",
                    Description = "Access every course for 1 year. Best value.",
                    Price = 3999,
                    DurationDays = 365,
                    IsActive = true
                });

            // ---------- Courses ----------
            Course csharpCourse = new Course
            {
                Instructor = instructor,
                Category = development,
                Title = "C# for Complete Beginners",
                Subtitle = "Learn C# programming from scratch with simple examples",
                Description = "This course takes you from zero to writing your own C# programs.\n\nWe start with variables and data types, then move to conditions, loops, methods and classes. Every lecture is short and practical.",
                WhatYouWillLearn = "Write your first C# program\nUnderstand variables, loops and conditions\nCreate your own classes and objects\nBuild a small console project",
                Requirements = "A computer with Windows, Mac or Linux\nNo programming experience needed",
                Language = "English",
                Level = "Beginner",
                Price = 0,
                ThumbnailUrl = "https://res.cloudinary.com/demo/image/upload/cld-sample-2.jpg",
                PromoVideoUrl = DemoVideoDog,
                IsPublished = true,
                PublishedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            csharpCourse.Sections.Add(MakeSection(1, "Getting Started",
                MakeLecture(1, "Welcome to the course", DemoVideoDog, 13, true),
                MakeLecture(2, "Installing Visual Studio", DemoVideoTurtle, 15, false)));
            csharpCourse.Sections.Add(MakeSection(2, "C# Basics",
                MakeLecture(1, "Variables and data types", DemoVideoElephants, 52, false),
                MakeLecture(2, "If and else", DemoVideoDog, 13, false),
                MakeLecture(3, "Loops", DemoVideoTurtle, 15, false)));

            Course reactCourse = new Course
            {
                Instructor = instructor,
                Category = development,
                Title = "React - The Practical Guide",
                Subtitle = "Build modern web apps with React, hooks and React Router",
                Description = "Learn React by building real projects.\n\nYou will understand components, props, state, effects and routing, and you will connect React to a .NET Web API.",
                WhatYouWillLearn = "Build React components\nManage state with useState and useEffect\nAdd pages with React Router\nCall a REST API with axios",
                Requirements = "Basic HTML, CSS and JavaScript",
                Language = "English",
                Level = "Intermediate",
                Price = 1999,
                ThumbnailUrl = "https://res.cloudinary.com/demo/image/upload/cld-sample-4.jpg",
                PromoVideoUrl = DemoVideoTurtle,
                IsPublished = true,
                PublishedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            reactCourse.Sections.Add(MakeSection(1, "Introduction",
                MakeLecture(1, "What is React?", DemoVideoTurtle, 15, true),
                MakeLecture(2, "Creating a project with Vite", DemoVideoDog, 13, false)));
            reactCourse.Sections.Add(MakeSection(2, "Components and Props",
                MakeLecture(1, "Your first component", DemoVideoElephants, 52, false),
                MakeLecture(2, "Passing data with props", DemoVideoDog, 13, false)));
            reactCourse.Sections.Add(MakeSection(3, "State and Effects",
                MakeLecture(1, "useState hook", DemoVideoTurtle, 15, false),
                MakeLecture(2, "useEffect hook", DemoVideoElephants, 52, false)));

            Course designCourse = new Course
            {
                Instructor = instructor,
                Category = design,
                Title = "UI Design Fundamentals",
                Subtitle = "Colors, typography and layout for beautiful apps",
                Description = "A short course about the basic rules of good user interface design.",
                WhatYouWillLearn = "Pick a color palette\nChoose fonts that work together\nUse spacing and alignment",
                Requirements = "No experience needed",
                Language = "English",
                Level = "All Levels",
                Price = 1299,
                ThumbnailUrl = "https://res.cloudinary.com/demo/image/upload/cld-sample-5.jpg",
                PromoVideoUrl = DemoVideoElephants,
                IsPublished = true,
                PublishedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            designCourse.Sections.Add(MakeSection(1, "Design Basics",
                MakeLecture(1, "Why design matters", DemoVideoElephants, 52, true),
                MakeLecture(2, "Colors", DemoVideoDog, 13, false),
                MakeLecture(3, "Typography", DemoVideoTurtle, 15, false)));

            db.Courses.AddRange(csharpCourse, reactCourse, designCourse);

            db.SaveChanges();

            // 5 more demo courses (marketing, business, photography, music, cooking).
            DemoCourses.AddMissing(db);
        }

        private static Section MakeSection(int order, string title, params Lecture[] lectures)
        {
            Section section = new Section
            {
                Title = title,
                OrderIndex = order
            };
            section.Lectures.AddRange(lectures);
            return section;
        }

        private static Lecture MakeLecture(int order, string title, string videoUrl, int seconds, bool isFreePreview)
        {
            return new Lecture
            {
                Title = title,
                Description = "In this lecture: " + title.ToLower() + ".",
                VideoUrl = videoUrl,
                DurationSeconds = seconds,
                IsFreePreview = isFreePreview,
                OrderIndex = order
            };
        }
    }
}
