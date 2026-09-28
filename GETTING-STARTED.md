# SmartLearn: a Udemy-style learning platform

An online course platform with an **ASP.NET Core Web API** backend and a **React**
frontend. The code is kept simple on purpose, so it is easy to read and learn from.

- **Instructors** create courses, split them into **sections and lectures**, upload
  videos and images (stored on **Cloudinary**), and publish them.
- **Students** browse and search courses, enroll in free courses, buy single courses,
  or buy a **subscription** that unlocks every course until it expires.
- **Progress tracking**: the player remembers where you stopped in each video, ticks
  lectures when they are completed, shows a progress bar, and gives a certificate at 100%.
- **Admins** see platform statistics and manage users, categories and subscription plans.
- **Emails**: sign up is verified with a 6-digit code (OTP), "Forgot password?" resets the
  password with a code, and students get an email when they enroll or buy a course or plan.

> This project lives next to the older MVC app in `src/`. The two do not share code or a database.

## Folder structure

```
backend/                      ASP.NET Core Web API (.NET 10)
  Controllers/                API endpoints, one class per feature
  Models/                     Database tables (Entity Framework classes)
  Dtos/                       Shapes of the JSON sent to / from the React app
  Services/                   Login tokens, file uploads, access rules, emails and codes
  Data/                       DbContext, demo data (DbSeeder), migrations
  appsettings.json            <- YOUR PASSWORDS AND KEYS GO HERE
frontend/                     React app (Vite)
  src/pages/                  One file per page
  src/pages/instructor/       Course editor (details, media, curriculum, publish)
  src/pages/admin/            Admin panel
  src/components/             Navbar, course card, page header, code input, skeletons, ...
  src/api.js                  Axios setup (adds the login token to every request)
  src/AuthContext.jsx         Keeps the logged in user (login, sign up, verify, reset)
  src/ToastContext.jsx        Small pop-up messages ("Saved!")
  src/index.css               All styles
```

## 1. Install the tools

| Tool | Version | Download |
| --- | --- | --- |
| .NET SDK | 10 | https://dotnet.microsoft.com/download |
| Node.js | 20 or newer | https://nodejs.org |
| PostgreSQL | 13 or newer | https://www.postgresql.org/download |

## 2. Add your personal settings

Open **`backend/appsettings.json`**. Every value you must change is marked with a
comment and starts with `YOUR_`:

| Setting | What to put there |
| --- | --- |
| `ConnectionStrings:DefaultConnection` → `Password=YOUR_POSTGRES_PASSWORD` | The password you chose when you installed PostgreSQL |
| `Jwt:Key` | Any long random text (at least 32 characters) |
| `Cloudinary:CloudName`, `ApiKey`, `ApiSecret` | Your Cloudinary keys (see below). **Optional.** |
| `Email:BrevoApiKey`, `Email:SenderEmail` | Your Brevo key and verified sender email (see below). **Optional.** |

**Cloudinary is optional.** While the Cloudinary values still start with `YOUR_`,
uploaded files are saved in `backend/wwwroot/uploads/` instead. So you can try
everything first and add Cloudinary later.

### Getting free Cloudinary keys

1. Create a free account: https://cloudinary.com/users/register_free
2. In the Cloudinary console, open **Settings → API Keys**.
3. Copy **Cloud name**, **API Key** and **API Secret** into `appsettings.json`.
4. Restart the backend. New uploads now go to your Cloudinary account (folder `smartlearn/`).

The free plan accepts **videos up to 100 MB each**.

### Getting a free Brevo key (for emails)

Emails (sign-up codes, password reset codes, enrollment and subscription confirmations)
are sent with [Brevo](https://www.brevo.com). The free plan sends 300 emails per day.
Brevo is used instead of Gmail SMTP because Railway blocks SMTP on its Trial and Hobby plans,
while Brevo is called over normal HTTPS.

1. Create a free account at https://www.brevo.com.
2. Go to **Senders, Domains & Dedicated IPs → Senders**, add your email address (for example
   your Gmail) and confirm it from the email Brevo sends you.
3. Go to **SMTP & API → API Keys**, click **Generate a new API key**, and copy it.
4. Put the key in `Email:BrevoApiKey` and your verified address in `Email:SenderEmail`.

**Brevo is optional while testing.** Without it, nothing is emailed: every email, including
the 6-digit code, is written to the backend console instead, so you can still sign up.

> Emails sent "from" a Gmail address through another service sometimes land in spam.
> Tell testers to check the spam folder, or verify your own domain in Brevo later.

> **Don't push real passwords or keys to a public GitHub repo.** A safer option is .NET
> "user secrets", which keeps them outside the project folder:
> ```
> cd backend
> dotnet user-secrets init
> dotnet user-secrets set "Cloudinary:ApiSecret" "your-secret"
> ```

## 3. Run the backend

```
cd backend
dotnet run
```

The API starts on **http://localhost:5000**. On the first run it creates the
`smartlearn_db` database and fills it with demo data: 3 users, 8 categories,
3 subscription plans and 3 sample courses. The sample videos come from Cloudinary's
public demo account.

## 4. Run the frontend

Open a second terminal:

```
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173**.

If your backend runs on another address, copy `frontend/.env.example` to
`frontend/.env` and change `VITE_API_URL`.

## Demo accounts

All three use the password **`Password@123`**. The login page also has buttons that fill them in.

| Email | Role |
| --- | --- |
| `student@smartlearn.dev` | Student |
| `instructor@smartlearn.dev` | Instructor (owns the 3 sample courses) |
| `admin@smartlearn.dev` | Admin |

## How access to courses works

The rules live in `backend/Services/CourseAccessService.cs`.

| How the student joined | Access |
| --- | --- |
| Free course (price 0) | Forever |
| Bought the single course | Forever |
| Joined with a subscription | Only while a subscription is active |

- When a subscription ends, those courses show **"Subscription expired"** in
  *My learning*. They open again after renewing. Progress is kept.
- Buying a plan while one is still active **adds the days to the end** of the current one.
- Buying a course that you joined with a subscription turns it into a lifetime purchase.
- Anyone can watch lectures marked **Free preview** from the course page.

**Payments are a demo.** The checkout page simulates a successful payment. See the
comment at the top of `backend/Controllers/CheckoutController.cs` for the steps to
connect Razorpay or Stripe later.

## How progress tracking works

- While a video plays, the player saves the position every 10 seconds and when you pause
  (`POST /api/learn/lectures/{id}/progress`). Next time, the video resumes from there.
- A lecture counts as **completed** when 90% is watched, when the video ends, or when
  the student ticks its checkbox. At the end of a video, the next lecture opens by itself.
- Course progress = completed lectures ÷ all lectures. At 100% the course is marked
  complete and the certificate page opens up (`/certificate/{courseId}`, printable to PDF).

## API endpoints

| Method | URL | Who |
| --- | --- | --- |
| POST | `/api/auth/register`, `/api/auth/verify-email`, `/api/auth/resend-code`, `/api/auth/login` | anyone |
| POST | `/api/auth/forgot-password`, `/api/auth/reset-password` | anyone |
| GET | `/api/auth/me` | logged in |
| GET | `/api/courses?search=&categoryId=&level=&price=free\|paid&sort=&page=` | anyone |
| GET | `/api/courses/{id}` (landing page + curriculum) | anyone |
| GET / POST | `/api/courses/{id}/reviews` | anyone / enrolled students |
| GET | `/api/categories`, `/api/plans` | anyone |
| POST | `/api/enrollments/{courseId}` (free course or with a subscription) | logged in |
| GET | `/api/enrollments/my` | logged in |
| GET | `/api/learn/{courseId}`, `/api/learn/{courseId}/certificate` | logged in with access |
| POST | `/api/learn/lectures/{id}/progress`, `/api/learn/lectures/{id}/complete` | logged in with access |
| POST | `/api/checkout/course/{id}`, `/api/checkout/plan/{id}` | logged in |
| GET | `/api/account/subscription`, `/api/account/payments` | logged in |
| PUT / POST | `/api/account/profile`, `/api/account/photo`, `/api/account/change-password` | logged in |
| GET | `/api/instructor/dashboard` | instructor |
| POST / PUT / DELETE | `/api/instructor/courses/{id}` (+ `/thumbnail`, `/promo-video`, `/publish`, `/unpublish`) | instructor |
| POST / PUT / DELETE | `/api/instructor/courses/{id}/sections`, `/api/instructor/sections/{id}` (+ `/move`) | instructor |
| POST / PUT / DELETE | `/api/instructor/sections/{id}/lectures`, `/api/instructor/lectures/{id}` (+ `/video`, `/move`) | instructor |
| GET / PUT | `/api/admin/stats`, `/api/admin/users`, `/api/admin/users/{id}/role`, `/api/admin/courses` | admin |
| POST / PUT / DELETE | `/api/categories/{id}`, `/api/plans/{id}` | admin |

## Changing the database

After you change a class in `backend/Models`, create a migration:

```
cd backend
dotnet tool restore
dotnet ef migrations add DescribeYourChange --output-dir Data/Migrations
```

The backend applies new migrations automatically when it starts.

To start over with fresh demo data, delete the database and run the backend again:
`DROP DATABASE smartlearn_db;` in psql or pgAdmin.

## Deploying (free), with automatic updates on every push

| Part | Service | Why |
| --- | --- | --- |
| Database | [Neon](https://neon.tech) | Free PostgreSQL |
| Backend | [Render](https://render.com) | Runs `backend/Dockerfile`. Redeploys on every push |
| Frontend | [Vercel](https://vercel.com) | Builds the React app. Redeploys on every push |
| Videos & images | [Cloudinary](https://cloudinary.com) | **Required** in production: Render's disk is wiped on every deploy |

1. **Neon**: create a project, then open **Connect** and choose the **.NET** format. Copy the
   connection string (it looks like `Host=...;Database=...;Username=...;Password=...;SSL Mode=...`).
2. **Render**: go to **New → Web Service** and pick this GitHub repo. Set **Language: Docker**,
   **Root Directory: `backend`**, **Branch: `main`** and **Instance type: Free**. Add these environment variables:

   | Key | Value |
   | --- | --- |
   | `ConnectionStrings__DefaultConnection` | the Neon connection string |
   | `Jwt__Key` | a long random secret (32+ characters) |
   | `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret` | your Cloudinary keys |
   | `Email__BrevoApiKey`, `Email__SenderEmail` | your Brevo key and verified sender (without them, sign-up codes only appear in the server log) |
   | `DemoPassword` | a strong password for the 3 demo accounts (including the admin) |
   | `FrontendUrl` | your Vercel address (step 4) |

   The `__` (two underscores) stands for a nested setting in `appsettings.json`.
   After the deploy, open `https://<your-service>.onrender.com/api/courses` to check that it works.
3. **Vercel**: go to **Add New → Project** and import this repo. Set **Root Directory: `frontend`**
   (Vite is detected automatically). Add the environment variable `VITE_API_URL` = your Render address,
   e.g. `https://smartlearn-api.onrender.com` (no `/api`, no trailing `/`).
4. Copy your Vercel address (e.g. `https://smartlearn.vercel.app`) into Render's `FrontendUrl`
   (no trailing `/`), then save. Render redeploys.

From then on, `git push` to `main` rebuilds and redeploys both parts automatically.
Database changes are applied when the backend starts, so commit your migration with the model change.

Notes:
- On the free plan, Render puts the backend to sleep after about 15 idle minutes. The next
  visit then takes around a minute to wake it.
- When you change a Vercel environment variable, redeploy for it to take effect.
- The demo-account buttons on the login page appear only on your computer (`npm run dev`).

## Security notes

- Changing a password, or an admin changing someone's role, logs that user out everywhere
  (older login tokens stop working).
- Sign-up and reset codes expire after 10 minutes, allow 5 wrong tries, and can be re-sent once a minute.
- "Forgot password" gives the same answer whether an email is registered or not.

## Known limitations

- Payments are simulated (see above).
- Video links are only sent to users who may watch the course. But anyone who
  already has a link can play it. For stronger protection, use Cloudinary's
  "authenticated" delivery type with signed URLs.
- The login token is stored in `localStorage` and lasts 24 hours (`Jwt:ExpiryHours`).
