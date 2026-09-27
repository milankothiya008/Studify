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

> This project lives next to the older MVC app in `src/`. The two do not share code or a database.

## Folder structure

```
backend/                      ASP.NET Core Web API (.NET 10)
  Controllers/                API endpoints, one class per feature
  Models/                     Database tables (Entity Framework classes)
  Dtos/                       Shapes of the JSON sent to / from the React app
  Services/                   Login tokens, file uploads, access rules
  Data/                       DbContext, demo data (DbSeeder), migrations
  appsettings.json            <- YOUR PASSWORDS AND KEYS GO HERE
frontend/                     React app (Vite)
  src/pages/                  One file per page
  src/pages/instructor/       Course editor (details, media, curriculum, publish)
  src/pages/admin/            Admin panel
  src/components/             Navbar, course card, rating stars, ...
  src/api.js                  Axios setup (adds the login token to every request)
  src/AuthContext.jsx         Keeps the logged in user
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

**Cloudinary is optional.** While the Cloudinary values still start with `YOUR_`,
uploaded files are saved in `backend/wwwroot/uploads/` instead. So you can try
everything first and add Cloudinary later.

### Getting free Cloudinary keys

1. Create a free account: https://cloudinary.com/users/register_free
2. In the Cloudinary console, open **Settings → API Keys**.
3. Copy **Cloud name**, **API Key** and **API Secret** into `appsettings.json`.
4. Restart the backend. New uploads now go to your Cloudinary account (folder `smartlearn/`).

The free plan accepts **videos up to 100 MB each**.

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
| POST | `/api/auth/register`, `/api/auth/login` | anyone |
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

## Known limitations

- Payments are simulated (see above).
- Video links are only sent to users who may watch the course. But anyone who
  already has a link can play it. For stronger protection, use Cloudinary's
  "authenticated" delivery type with signed URLs.
- The login token is stored in `localStorage` and lasts 24 hours (`Jwt:ExpiryHours`).
