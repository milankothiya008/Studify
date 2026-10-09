using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartLearning.Api.Models;

namespace SmartLearning.Api.Data
{
    // Demo people and activity, so every feature has something to show in a presentation:
    // students with progress and certificates, purchases with coupons, subscriptions
    // (active and expired), reviews, Q&A, notes, quiz results, wishlists, followers,
    // notifications and sales for the instructor analytics chart.
    //
    // Run it from the admin panel ("Add demo activity"). It runs only once:
    // if the demo students already exist, nothing is added.
    //
    // All demo accounts get the same password as the demo student (student@smartlearn.dev).
    public static class DemoActivity
    {
        private const string MarkerEmail = "aarav@smartlearn.dev";

        // Course titles from DbSeeder and DemoCourses.
        private const string CSharp = "C# for Complete Beginners";
        private const string React = "React - The Practical Guide";
        private const string UiDesign = "UI Design Fundamentals";
        private const string Marketing = "Digital Marketing Essentials";
        private const string Leadership = "Leadership and Teamwork Skills";
        private const string Photography = "Smartphone Photography Made Easy";
        private const string Jazz = "Jazz Improvisation for Beginners";
        private const string Cooking = "Cooking Basics: Cook Like a Chef at Home";

        // One planned enrollment of a demo student.
        private class PlannedEnrollment
        {
            public string CourseTitle;
            public string AccessType;
            public int Percent;        // how much of the course is done
            public int DaysAgo;        // when the student joined
            public int Rating;         // 0 = no review
            public string Comment;
            public bool UseCoupon;     // bought with WELCOME20
        }

        private static PlannedEnrollment Join(string title, string access, int percent, int daysAgo,
            int rating = 0, string comment = null, bool useCoupon = false)
        {
            return new PlannedEnrollment
            {
                CourseTitle = title,
                AccessType = access,
                Percent = percent,
                DaysAgo = daysAgo,
                Rating = rating,
                Comment = comment,
                UseCoupon = useCoupon
            };
        }

        // Adds everything. Returns a message for the admin.
        public static string Add(AppDbContext db)
        {
            if (db.Users.Any(u => u.Email == MarkerEmail))
            {
                return "Demo activity was already added.";
            }

            // Stop if one of the demo emails is already used by someone else.
            string[] demoEmails =
            {
                "priya@smartlearn.dev", "rahul@smartlearn.dev", "diya@smartlearn.dev", "rohan@smartlearn.dev",
                "ananya@smartlearn.dev", "kabir@smartlearn.dev", "meera@smartlearn.dev", "arjun@smartlearn.dev",
                "isha@smartlearn.dev"
            };
            string takenEmail = db.Users.Where(u => demoEmails.Contains(u.Email)).Select(u => u.Email).FirstOrDefault();
            if (takenEmail != null)
            {
                return "Cannot add demo activity: " + takenEmail + " is already used by another account.";
            }

            User sam = db.Users.FirstOrDefault(u => u.Email == "student@smartlearn.dev");
            User grace = db.Users.FirstOrDefault(u => u.Email == "instructor@smartlearn.dev");
            if (sam == null || grace == null)
            {
                return "The demo student and demo instructor accounts are needed first.";
            }

            List<Course> courses = db.Courses
                .Include(c => c.Instructor)
                .Include(c => c.Sections).ThenInclude(s => s.Lectures)
                .Include(c => c.Sections).ThenInclude(s => s.Quiz).ThenInclude(q => q.Questions)
                .ToList();

            DateTime now = DateTime.UtcNow;
            DemoBuilder builder = new DemoBuilder(db, courses, now);

            using IDbContextTransaction transaction = db.Database.BeginTransaction();

            // ---------- Instructors ----------
            User priya = builder.NewUser("Priya Nair", "priya@smartlearn.dev", Roles.Instructor, sam.PasswordHash, 120);
            priya.Headline = "Product designer at a fintech startup, 9 years in UX";
            priya.Bio = "I design apps used by millions of people every day.\n\n" +
                "In my courses you learn the way designers really work: start with the problem, sketch quickly, test with real users and polish at the end.";
            priya.WebsiteUrl = "https://example.com/";
            priya.LinkedInUrl = "https://www.linkedin.com/";

            User rahul = builder.NewUser("Rahul Mehta", "rahul@smartlearn.dev", Roles.Instructor, sam.PasswordHash, 110);
            rahul.Headline = "Marketing lead and team coach";
            rahul.Bio = "I have led marketing teams for 12 years and helped more than 50 small businesses grow online.\n\n" +
                "My lessons are short, practical and full of real campaign examples.";
            rahul.YouTubeUrl = "https://www.youtube.com/";
            rahul.TwitterUrl = "https://x.com/";

            if (string.IsNullOrEmpty(grace.WebsiteUrl))
            {
                grace.WebsiteUrl = "https://example.com/";
                grace.LinkedInUrl = "https://www.linkedin.com/";
            }

            // Give the new instructors some of the demo courses (only if the demo instructor still owns them).
            builder.MoveCourse(UiDesign, grace, priya);
            builder.MoveCourse(Photography, grace, priya);
            builder.MoveCourse(Marketing, grace, rahul);
            builder.MoveCourse(Leadership, grace, rahul);

            // The demo accounts and courses were created recently; move their dates back,
            // so nobody enrolls in a course before it existed.
            builder.Backdate(grace, 150);
            builder.Backdate(sam, 70);
            string[] demoTitles = { CSharp, React, UiDesign, Marketing, Leadership, Photography, Jazz, Cooking };
            for (int i = 0; i < demoTitles.Length; i++)
            {
                builder.BackdateCourse(demoTitles[i], 90 - i * 4);
            }

            // ---------- Students ----------
            User aarav = builder.NewUser("Aarav Sharma", MarkerEmail, Roles.Student, sam.PasswordHash, 60);
            User diya = builder.NewUser("Diya Patel", "diya@smartlearn.dev", Roles.Student, sam.PasswordHash, 55);
            User rohan = builder.NewUser("Rohan Gupta", "rohan@smartlearn.dev", Roles.Student, sam.PasswordHash, 50);
            User ananya = builder.NewUser("Ananya Iyer", "ananya@smartlearn.dev", Roles.Student, sam.PasswordHash, 45);
            User kabir = builder.NewUser("Kabir Singh", "kabir@smartlearn.dev", Roles.Student, sam.PasswordHash, 50);
            User meera = builder.NewUser("Meera Joshi", "meera@smartlearn.dev", Roles.Student, sam.PasswordHash, 40);
            User arjun = builder.NewUser("Arjun Reddy", "arjun@smartlearn.dev", Roles.Student, sam.PasswordHash, 35);
            User isha = builder.NewUser("Isha Verma", "isha@smartlearn.dev", Roles.Student, sam.PasswordHash, 30);

            // Save the people first, so everyone has an id (used in notification links).
            db.SaveChanges();

            // ---------- Subscriptions ----------
            builder.Subscribe(diya, "Yearly", 25, true);     // active, bought with SUBSCRIBE10
            builder.Subscribe(ananya, "Quarterly", 9, false); // active
            builder.Subscribe(kabir, "Monthly", 45, false);   // ended 15 days ago (expired)

            // ---------- Enrollments, progress, quizzes, payments and reviews ----------
            string free = AccessTypes.Free;
            string bought = AccessTypes.Purchased;
            string plan = AccessTypes.Subscription;

            builder.Enroll(sam, new List<PlannedEnrollment>
            {
                Join(CSharp, free, 100, 40, 5, "A perfect first course. Short lectures, clear examples and the quizzes made sure I really understood each part."),
                Join(React, bought, 60, 20, 0, null, true),
                Join(Cooking, bought, 25, 6)
            });
            builder.Enroll(aarav, new List<PlannedEnrollment>
            {
                Join(CSharp, free, 100, 35, 5, "Clear explanations and great pacing. I finally understand classes and objects."),
                Join(React, bought, 100, 30, 5, "The best React course I have taken. The projects are practical and the instructor answers questions quickly."),
                Join(UiDesign, bought, 45, 12)
            });
            builder.Enroll(diya, new List<PlannedEnrollment>
            {
                Join(React, plan, 80, 24, 4, "Very good content. I would love one more section about testing."),
                Join(UiDesign, plan, 100, 22, 5, "Priya explains design thinking so well. I redesigned my portfolio after this course!"),
                Join(Marketing, plan, 50, 10),
                Join(Jazz, plan, 20, 3)
            });
            builder.Enroll(rohan, new List<PlannedEnrollment>
            {
                Join(CSharp, free, 70, 28, 4, "Good course for beginners. Some exercises at the end of each section would make it perfect."),
                Join(Leadership, bought, 100, 26, 4, "Useful tips I could use with my team the very next day."),
                Join(Photography, free, 100, 15, 5, "My phone photos look so much better now. Loved the lighting section.")
            });
            builder.Enroll(ananya, new List<PlannedEnrollment>
            {
                Join(Cooking, plan, 100, 8, 5, "Fun, simple and the recipes actually work. My family is impressed!"),
                Join(Photography, free, 60, 5),
                Join(Marketing, plan, 30, 2)
            });
            builder.Enroll(kabir, new List<PlannedEnrollment>
            {
                Join(Jazz, plan, 40, 44),
                Join(CSharp, free, 100, 20, 3, "Good basics, but I wanted more practice exercises.")
            });
            builder.Enroll(meera, new List<PlannedEnrollment>
            {
                Join(Marketing, bought, 100, 18, 5, "Rahul shares real campaign examples. I used his checklist for my own small business.", true),
                Join(Leadership, bought, 30, 7),
                Join(Photography, free, 100, 14, 4, "Nice and short. The editing lecture was my favourite.")
            });
            builder.Enroll(arjun, new List<PlannedEnrollment>
            {
                Join(React, bought, 15, 4),
                Join(CSharp, free, 100, 16, 5, "Great introduction to programming. The quizzes are a nice touch."),
                Join(Jazz, bought, 100, 11, 4, "Relaxing and well explained. I can now improvise over a simple blues.")
            });
            builder.Enroll(isha, new List<PlannedEnrollment>
            {
                Join(UiDesign, bought, 70, 9, 4, "Practical and well organised. The wireframe exercises were the best part."),
                Join(Cooking, bought, 100, 13, 4, "Lovely course, easy to follow even for a complete beginner."),
                Join(CSharp, free, 10, 1)
            });

            // ---------- Followers ----------
            builder.Follow(sam, grace, 30);
            builder.Follow(sam, priya, 12);
            builder.Follow(aarav, grace, 34);
            builder.Follow(aarav, priya, 11);
            builder.Follow(diya, priya, 21);
            builder.Follow(diya, rahul, 9);
            builder.Follow(diya, grace, 20);
            builder.Follow(rohan, rahul, 25);
            builder.Follow(ananya, grace, 7);
            builder.Follow(meera, rahul, 17);
            builder.Follow(meera, priya, 13);
            builder.Follow(arjun, grace, 10);
            builder.Follow(isha, priya, 8);
            builder.Follow(isha, grace, 12);
            builder.Follow(priya, grace, 60);

            // ---------- Wishlists ----------
            builder.Wish(sam, Jazz);
            builder.Wish(sam, UiDesign);
            builder.Wish(aarav, Leadership);
            builder.Wish(rohan, React);
            builder.Wish(isha, Marketing);
            builder.Wish(arjun, UiDesign);

            // ---------- Q&A ----------
            Question q1 = builder.Ask(sam, React, 0, 14, "When should I use useEffect and when useMemo?",
                "Both seem to run code when a value changes. How do I choose between them?");
            builder.Reply(q1, grace, 13, "Great question! useMemo calculates a value while rendering (for example a filtered list). " +
                "useEffect runs after rendering, for side effects like loading data or changing the page title. " +
                "If you need a value to show, use useMemo; if you need to do something, use useEffect.");
            builder.Reply(q1, aarav, 12, "This helped me too. I use useMemo for sorting tables and useEffect for API calls.");

            Question q2 = builder.Ask(rohan, CSharp, 1, 20, "What is the difference between int and double?",
                "I get different results when I divide numbers. Why?");
            builder.Reply(q2, grace, 19, "int stores whole numbers, double stores numbers with decimals. 7 / 2 with ints gives 3, " +
                "but 7.0 / 2 gives 3.5. If you need decimals, make at least one number a double.");

            Question q3 = builder.Ask(isha, UiDesign, 0, 6, "Which tool should I use for wireframes?",
                "Is paper enough, or should I start with Figma right away?");
            builder.Reply(q3, priya, 5, "Start on paper: it is the fastest way to try many ideas. " +
                "When you know what you want, move to Figma to make it clean and clickable.");

            builder.Ask(diya, Marketing, 1, 2, "How do I measure the results of Instagram ads?",
                "Which numbers should I look at first: reach, clicks or sales?");
            builder.Ask(arjun, React, 1, 1, "My component renders twice in development",
                "I see my console.log two times. Is something wrong with my code?");

            // ---------- Notes ----------
            builder.Note(sam, React, 0, 42, "useState returns the value and a function to change it.", 18);
            builder.Note(sam, React, 1, 95, "Lists need a unique key on every item.", 15);
            builder.Note(sam, CSharp, 0, 30, "Every C# program starts in the Main method.", 38);
            builder.Note(aarav, React, 2, 60, "Lift state up to the closest common parent.", 25);

            db.SaveChanges();
            transaction.Commit();

            return "Demo activity added: 2 instructors, 8 students, " + builder.EnrollmentCount + " enrollments, " +
                builder.PaymentCount + " payments, " + builder.ReviewCount + " reviews and more. " +
                "Demo accounts use the same password as student@smartlearn.dev.";
        }

        // Small helpers that create the rows. Kept in one class so the plan above stays easy to read.
        private class DemoBuilder
        {
            private readonly AppDbContext _db;
            private readonly List<Course> _courses;
            private readonly DateTime _now;
            private readonly Coupon _courseCoupon;
            private readonly Coupon _planCoupon;

            public int EnrollmentCount;
            public int PaymentCount;
            public int ReviewCount;

            public DemoBuilder(AppDbContext db, List<Course> courses, DateTime now)
            {
                _db = db;
                _courses = courses;
                _now = now;
                _courseCoupon = db.Coupons.FirstOrDefault(c => c.Code == "WELCOME20" && c.IsActive);
                _planCoupon = db.Coupons.FirstOrDefault(c => c.Code == "SUBSCRIBE10" && c.IsActive);
            }

            private DateTime DaysAgo(double days)
            {
                return _now.AddDays(-days);
            }

            private Course FindCourse(string title)
            {
                return _courses.FirstOrDefault(c => c.Title == title);
            }

            private static string NewTransactionId()
            {
                return "DEMO-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
            }

            public User NewUser(string name, string email, string role, string passwordHash, int joinedDaysAgo)
            {
                User user = new User
                {
                    FullName = name,
                    Email = email,
                    PasswordHash = passwordHash,
                    Role = role,
                    IsEmailVerified = true,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    CreatedAt = DaysAgo(joinedDaysAgo)
                };
                _db.Users.Add(user);
                return user;
            }

            // Only moves dates back, never forward.
            public void Backdate(User user, int daysAgo)
            {
                DateTime date = DaysAgo(daysAgo);
                if (user.CreatedAt > date)
                {
                    user.CreatedAt = date;
                }
            }

            public void BackdateCourse(string title, int daysAgo)
            {
                Course course = FindCourse(title);
                DateTime date = DaysAgo(daysAgo);
                if (course == null || course.CreatedAt <= date)
                {
                    return;
                }
                course.CreatedAt = date;
                if (course.PublishedAt != null)
                {
                    course.PublishedAt = date.AddDays(1);
                }
            }

            public void MoveCourse(string title, User from, User to)
            {
                Course course = FindCourse(title);
                if (course != null && course.InstructorId == from.Id)
                {
                    course.Instructor = to;
                }
            }

            private void Notify(User user, string title, string message, string link, double daysAgo)
            {
                _db.Notifications.Add(new Notification
                {
                    User = user,
                    Title = title,
                    Message = message,
                    Link = link,
                    IsRead = daysAgo > 4, // older ones are already read
                    CreatedAt = DaysAgo(daysAgo)
                });
            }

            public void Subscribe(User user, string planName, int startedDaysAgo, bool useCoupon)
            {
                SubscriptionPlan plan = _db.SubscriptionPlans.FirstOrDefault(p => p.Name == planName);
                if (plan == null)
                {
                    return;
                }

                DateTime start = DaysAgo(startedDaysAgo);
                UserSubscription subscription = new UserSubscription
                {
                    User = user,
                    PlanId = plan.Id,
                    StartDate = start,
                    EndDate = start.AddDays(plan.DurationDays)
                };
                _db.UserSubscriptions.Add(subscription);

                Coupon coupon = useCoupon ? _planCoupon : null;
                AddPayment(user, "Subscription", null, plan.Id, plan.Price, coupon, start);

                Notify(user, "Subscription active",
                    "Your " + plan.Name + " plan is valid until " + subscription.EndDate.ToString("dd MMM yyyy") + ".",
                    "/courses", startedDaysAgo);
            }

            private void AddPayment(User user, string type, Course course, int? planId, decimal price, Coupon coupon, DateTime date)
            {
                // Respect the coupon's limit, like a real checkout does.
                if (coupon != null && coupon.MaxUses != null && coupon.UsedCount >= coupon.MaxUses)
                {
                    coupon = null;
                }

                decimal discount = coupon != null ? Math.Round(price * coupon.DiscountPercent / 100m, 2) : 0;
                if (coupon != null)
                {
                    coupon.UsedCount++;
                }

                _db.Payments.Add(new Payment
                {
                    User = user,
                    PaymentType = type,
                    Course = course,
                    PlanId = planId,
                    OriginalAmount = price,
                    DiscountAmount = discount,
                    Amount = price - discount,
                    Coupon = coupon,
                    Status = "Paid",
                    TransactionId = NewTransactionId(),
                    CreatedAt = date
                });
                PaymentCount++;
            }

            public void Enroll(User student, List<PlannedEnrollment> plans)
            {
                foreach (PlannedEnrollment planned in plans)
                {
                    Course course = FindCourse(planned.CourseTitle);
                    if (course == null || !course.IsPublished)
                    {
                        continue;
                    }

                    // Never touch an enrollment that already exists (for example from earlier testing).
                    if (_db.Enrollments.Any(e => e.UserId == student.Id && e.CourseId == course.Id))
                    {
                        continue;
                    }

                    // Free courses are always "Free"; paid courses use the planned access type.
                    string access = course.Price == 0 ? AccessTypes.Free : planned.AccessType;
                    if (access == AccessTypes.Free && course.Price > 0)
                    {
                        access = AccessTypes.Purchased;
                    }

                    DateTime enrolledAt = DaysAgo(planned.DaysAgo);
                    Enrollment enrollment = new Enrollment
                    {
                        User = student,
                        CourseId = course.Id,
                        AccessType = access,
                        EnrolledAt = enrolledAt
                    };
                    _db.Enrollments.Add(enrollment);
                    EnrollmentCount++;

                    if (access == AccessTypes.Purchased)
                    {
                        Coupon coupon = planned.UseCoupon ? _courseCoupon : null;
                        AddPayment(student, "Course", course, null, course.Price, coupon, enrolledAt);
                    }

                    AddProgress(student, course, enrollment, planned);

                    bool alreadyReviewed = _db.Reviews.Any(r => r.UserId == student.Id && r.CourseId == course.Id);
                    if (planned.Rating > 0 && !alreadyReviewed)
                    {
                        _db.Reviews.Add(new Review
                        {
                            User = student,
                            CourseId = course.Id,
                            Rating = planned.Rating,
                            Comment = planned.Comment,
                            CreatedAt = DaysAgo(planned.DaysAgo * 0.4)
                        });
                        ReviewCount++;
                    }

                    Notify(student, "You're enrolled!", "You can now start \"" + course.Title + "\".",
                        "/learn/" + course.Id, planned.DaysAgo);

                    string sale = access == AccessTypes.Purchased ? " (sale: ₹" + course.Price.ToString("N0") + ")" : "";
                    Notify(course.Instructor, "New student",
                        student.FullName + " enrolled in \"" + course.Title + "\"" + sale + ".", "/instructor", planned.DaysAgo);
                }
            }

            // Marks lectures as watched, one after another, until the planned percent is reached.
            // Quizzes of finished sections get a passed attempt.
            private void AddProgress(User student, Course course, Enrollment enrollment, PlannedEnrollment planned)
            {
                List<Section> sections = course.Sections.OrderBy(s => s.OrderIndex).ToList();
                List<Lecture> lectures = sections.SelectMany(s => s.Lectures.OrderBy(l => l.OrderIndex)).ToList();
                if (lectures.Count == 0 || planned.Percent == 0)
                {
                    return;
                }

                int doneCount = (int)Math.Round(lectures.Count * planned.Percent / 100.0);
                if (doneCount == 0)
                {
                    doneCount = 1;
                }
                if (planned.Percent < 100 && doneCount >= lectures.Count)
                {
                    doneCount = lectures.Count - 1;
                }

                // Lectures that already have a progress row are left as they are (one row per lecture).
                HashSet<int> existing = _db.LectureProgresses
                    .Where(p => p.UserId == student.Id)
                    .Select(p => p.LectureId)
                    .ToHashSet();

                // Spread the watching over the time since enrolling.
                double span = Math.Max(planned.DaysAgo - 0.5, 0.2);
                for (int i = 0; i < doneCount; i++)
                {
                    if (existing.Contains(lectures[i].Id))
                    {
                        continue;
                    }
                    _db.LectureProgresses.Add(new LectureProgress
                    {
                        User = student,
                        LectureId = lectures[i].Id,
                        WatchedSeconds = lectures[i].DurationSeconds,
                        IsCompleted = true,
                        UpdatedAt = DaysAgo(planned.DaysAgo - span * (i + 1) / doneCount)
                    });
                }

                Lecture last = lectures[doneCount - 1];
                if (doneCount < lectures.Count && !existing.Contains(lectures[doneCount].Id))
                {
                    // The next lecture is half watched: "Continue" opens it.
                    last = lectures[doneCount];
                    _db.LectureProgresses.Add(new LectureProgress
                    {
                        User = student,
                        LectureId = last.Id,
                        WatchedSeconds = last.DurationSeconds / 2,
                        IsCompleted = false,
                        UpdatedAt = DaysAgo(0.3)
                    });
                }

                enrollment.LastLectureId = last.Id;
                enrollment.LastAccessedAt = DaysAgo(planned.Percent == 100 ? planned.DaysAgo * 0.5 : 0.3);
                if (doneCount == lectures.Count)
                {
                    enrollment.CompletedAt = DaysAgo(planned.DaysAgo * 0.5);
                }

                // Quizzes of sections where every lecture is done.
                List<int> doneIds = lectures.Take(doneCount).Select(l => l.Id).ToList();
                int sectionNumber = 0;
                foreach (Section section in sections)
                {
                    sectionNumber++;
                    bool sectionDone = section.Lectures.Count > 0 && section.Lectures.All(l => doneIds.Contains(l.Id));
                    if (!sectionDone || section.Quiz == null || section.Quiz.Questions.Count == 0)
                    {
                        continue;
                    }

                    int total = section.Quiz.Questions.Count;
                    int wanted = 70 + (student.FullName.Length * 7 + sectionNumber * 11) % 31; // 70 - 100
                    int correct = Math.Max(1, (int)Math.Round(total * wanted / 100.0));
                    int score = correct * 100 / total;

                    _db.QuizAttempts.Add(new QuizAttempt
                    {
                        QuizId = section.Quiz.Id,
                        User = student,
                        CorrectCount = correct,
                        TotalQuestions = total,
                        ScorePercent = score,
                        Passed = score >= section.Quiz.PassPercent,
                        CreatedAt = DaysAgo(planned.DaysAgo * 0.6)
                    });
                }
            }

            public void Follow(User follower, User instructor, int daysAgo)
            {
                if (_db.Follows.Any(f => f.FollowerId == follower.Id && f.InstructorId == instructor.Id))
                {
                    return;
                }

                _db.Follows.Add(new Follow { FollowerId = follower.Id, InstructorId = instructor.Id, CreatedAt = DaysAgo(daysAgo) });
                Notify(instructor, "New follower", follower.FullName + " started following you.",
                    "/instructors/" + instructor.Id, daysAgo);
            }

            public void Wish(User user, string title)
            {
                Course course = FindCourse(title);
                if (course == null || !course.IsPublished)
                {
                    return;
                }
                if (_db.WishlistItems.Any(w => w.UserId == user.Id && w.CourseId == course.Id))
                {
                    return;
                }
                _db.WishlistItems.Add(new WishlistItem { User = user, CourseId = course.Id, CreatedAt = DaysAgo(3) });
            }

            // lectureIndex = which lecture of the course the question is about (0 = first).
            public Question Ask(User student, string title, int lectureIndex, int daysAgo, string questionTitle, string body)
            {
                Course course = FindCourse(title);
                if (course == null)
                {
                    return null;
                }

                Lecture lecture = course.Sections.OrderBy(s => s.OrderIndex)
                    .SelectMany(s => s.Lectures.OrderBy(l => l.OrderIndex))
                    .Skip(lectureIndex)
                    .FirstOrDefault();

                Question question = new Question
                {
                    CourseId = course.Id,
                    LectureId = lecture != null ? lecture.Id : null,
                    User = student,
                    Title = questionTitle,
                    Body = body,
                    CreatedAt = DaysAgo(daysAgo)
                };
                _db.Questions.Add(question);
                return question;
            }

            public void Reply(Question question, User author, int daysAgo, string body)
            {
                if (question == null)
                {
                    return;
                }

                question.Answers.Add(new Answer { User = author, Body = body, CreatedAt = DaysAgo(daysAgo) });

                Course course = _courses.First(c => c.Id == question.CourseId);
                bool isInstructor = course.InstructorId == author.Id;
                string who = isInstructor ? "The instructor" : author.FullName;
                Notify(question.User, "New answer to your question", who + " answered \"" + question.Title + "\".",
                    "/learn/" + question.CourseId, daysAgo);
            }

            public void Note(User student, string title, int lectureIndex, int seconds, string text, int daysAgo)
            {
                Course course = FindCourse(title);
                if (course == null)
                {
                    return;
                }

                Lecture lecture = course.Sections.OrderBy(s => s.OrderIndex)
                    .SelectMany(s => s.Lectures.OrderBy(l => l.OrderIndex))
                    .Skip(lectureIndex)
                    .FirstOrDefault();
                if (lecture == null)
                {
                    return;
                }

                // A note's time must be inside the video.
                int time = lecture.DurationSeconds > 0 ? Math.Min(seconds, Math.Max(lecture.DurationSeconds - 1, 0)) : 0;
                _db.Notes.Add(new Note
                {
                    User = student,
                    CourseId = course.Id,
                    LectureId = lecture.Id,
                    Seconds = time,
                    Text = text,
                    CreatedAt = DaysAgo(daysAgo)
                });
            }
        }
    }
}
