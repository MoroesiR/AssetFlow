# AssetFlow - IT Asset Management System

A web-based system for tracking IT equipment, managing checkouts, and scheduling maintenance. Built with ASP.NET Core MVC as part of my journey learning full-stack development.

<img width="1867" height="907" alt="image" src="https://github.com/user-attachments/assets/88d0fed0-9ebc-4274-bda1-82132c7a6489" />


## Why I Built This

At my current workplace, I saw how much time gets wasted tracking equipment manually - who has what laptop, when was the last printer maintenance, which projector is available for the conference room. I thought, "there has to be a better way to do this," so I built one.

This project helped me learn ASP.NET Core while solving a real problem. Every feature here came from thinking about actual IT department workflows.

## What It Does

### Current Features

**Asset Management**
- Add new equipment with details (serial number, category, purchase info, warranty dates)
- Track location and status of all assets
- Prevents duplicate serial numbers
- Categories include IT Equipment, Furniture, AV Equipment, Office Supplies

**Check-In/Check-Out System**
- Admin checks out assets to employees
- Tracks who has what, from which department
- Shows expected return dates with overdue indicators (the red "17 days overdue" warning you see in screenshots)
- Admin marks condition when equipment comes back
- Flags items that need maintenance during return

**Maintenance Tracking**
- Schedule maintenance with due dates
- Color-coded urgency (red = overdue, coming soon: yellow for due soon)
- Track maintenance history and notes
- Quick toggle between maintenance and available status

**Dashboard & Reports**
- Real-time metrics (total assets, available count, checked out, needing maintenance)
- Asset value by category with interactive pie chart visualization
- Summary table showing count and total value per category
- Export to CSV for external analysis
- Print-friendly report generation
- Checkout history with date ranges
- Maintenance schedule showing overdue items

**Roles and the Request Workflow**
- Two roles: Admin (IT department) and Employee
- Employees sign up with their name and department, and land on their own portal
- Employees browse what is available, submit a request with dates and a reason, and can cancel while it is still pending
- Admin gets a request queue with a live count in the navigation, oldest first
- Approving a request checks the asset out to the requester in one step
- Rejections need a reason, and the requester sees it on their request list
- Employees can see exactly what is signed out to them and when it is due back
- Inventory, reports, maintenance and the dashboard are admin-only

**Notifications**
- A bell in the navigation with an unread count, for both roles
- Admin is told when a request comes in and when one is withdrawn
- The requester is told when their request is approved or declined, with the reason
- Overdue equipment and items due for servicing are raised automatically
- Clicking a notification marks it read and takes you to the thing it is about
- Mark all read, and clear the ones you have read - unread items are never dropped

**Bulk Import**
- Add many assets at once from a CSV, with a downloadable template
- Tick "check the file without importing" to validate first - nothing is written
- A bad row is skipped with a reason and a line number, the rest still import
- Duplicate serials are caught against the database and within the file itself
- Prices are read in either convention, so R 4 250,00 and 4,250.00 both mean the same thing

**Search and Filtering**
- Search across name, serial, location, vendor and whoever is holding the item
- Filter by status, category, vendor, holding department and a price range
- Saved views for overdue, maintenance due, expired warranty and missing location
- Sort by name, price or purchase date
- Category and vendor dropdowns are built from the data, not hardcoded

**API Access**
- RESTful endpoints for all major operations
- Swagger/OpenAPI documentation for testing
- Search and filter capabilities
- JSON responses for potential integrations

## Screenshots

- Home page with quick actions
  <img width="1867" height="907" alt="image" src="https://github.com/user-attachments/assets/124cc937-be8b-49ab-864b-bd6008ba9a23" />

- Dashboard with charts
  <img width="1869" height="901" alt="image" src="https://github.com/user-attachments/assets/496d1bc8-f2b1-44aa-a9d9-074f27b64d19" />

  <img width="1885" height="724" alt="Screenshot 2026-02-16 090155" src="https://github.com/user-attachments/assets/d500535a-e46f-46d7-9e30-15b0bd437d1e" />


- Assets inventory view
  <img width="1844" height="903" alt="image" src="https://github.com/user-attachments/assets/d9e90e8d-0fa7-41d1-89d9-ff6cbe349ebf" />

- Maintenance schedule with overdue items
  <img width="1862" height="676" alt="Screenshot 2026-02-16 085410" src="https://github.com/user-attachments/assets/260c2825-52ee-4636-b262-dfedb593fd6f" />

- API documentation page
  <img width="1855" height="846" alt="image" src="https://github.com/user-attachments/assets/3c7f46db-af2a-4ae9-b396-536ff327cc65" />


## Tech Stack

- **Backend:** ASP.NET Core 10.0, C#
- **Database:** SQL Server (SQLite for dev/testing)
- **ORM:** Entity Framework Core
- **Auth:** ASP.NET Core Identity
- **Frontend:** Razor Pages, Bootstrap 5, custom CSS
- **Charts:** Chart.js for data visualization
- **API Docs:** Swashbuckle/Swagger

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- SQL Server (or use SQLite for quick testing)
- Visual Studio 2022 or VS Code

### Installation

1. Clone the repo
```bash
git clone https://github.com/MoroesiR/assetflow.git
cd assetflow
```

2. Update the database
```bash
dotnet ef database update
```

3. Run the application
```bash
dotnet run
```
Or just hit F5 in Visual Studio

4. Open your browser to `https://localhost:5001`

### Demo Login
The admin account is created on first run from the `AdminUser` section in
`appsettings.json`. For anything beyond local testing move those values into
user secrets or environment variables.

- **Email:** admin@gmail.com
- **Password:** Admin@123

For the employee side, register a new account through the Register link. Anyone
who signs up gets the Employee role - admin rights are only handed out from the
seeder or by hand.


## Current Limitations & Known Issues

The admin still does all the physical work - handing equipment over, checking it
back in, scheduling maintenance. Employees can now ask for equipment themselves
instead of walking to the IT office, but nothing is self-service beyond that.

**Known limitations:**
- Notifications are in-app only. There is no mail or SMS gateway behind this, so nothing reaches you when you are signed out
- The overdue and maintenance sweep runs when somebody opens the bell, not on a timer, so a notice appears on first visit rather than overnight
- Approving one request does not automatically reject the others waiting on the same asset - the admin is warned and decides
- Can't reserve equipment in advance
- Maintenance schedule doesn't auto-generate recurring tasks
- The API endpoints are still open - authentication on them is on the list

## Future Features

Phase 1 (the multi-role request system) is done and described under Current
Features. These are next:

### Phase 2: Enhanced Features
- Email or SMS delivery for the notifications that already exist in-app
- Equipment reservation system (book for future dates)
- Recurring maintenance schedules
- Mobile-responsive improvements

### Phase 3: Advanced Analytics
- Usage analytics (most/least used equipment, utilization rates)
- Department-wise asset allocation reports
- Depreciation tracking and asset lifecycle costs
- Checkout trends over time (monthly/quarterly graphs)
- Custom report builder with user-defined filters
- Equipment idle time analysis

## What I Learned Building This

**Technical stuff:**
- Entity Framework relationships are trickier than they look - spent a whole afternoon figuring out why my cascade deletes weren't working
- ASP.NET Identity is powerful but the documentation assumes you know more than you do
- State management for the asset lifecycle (Available → Checked Out → Maintenance → Available) needed more thought than I expected
- Swagger is amazing for API testing - wish I'd set it up earlier
- Bootstrap is great until you need something custom, then you're writing CSS anyway
- Swapping IdentityUser for my own ApplicationUser halfway through the project meant touching the context, the seeder and every page that injected the old type. Next time I'll extend the user on day one
- Hiding a nav link is not security. The role checks had to go on the controllers, and I only trusted them once I'd tried typing the admin URLs in as an employee

**Design decisions:**
- Started with too many features in mind, had to scale back to get something working first
- User workflows are harder to design than the actual code
- Validation is boring but absolutely necessary (learned this when test data broke everything)
- Color coding makes a huge difference in usability

**Challenges I overcame:**
- Figuring out how to prevent duplicate serial numbers while still allowing edits
- Making the checkout history show actual return status (not just dates)
- Getting the maintenance "days overdue" calculation to work properly
- Chart.js integration took longer than expected (JavaScript + Razor Pages = confusion at first)
- Deployment readiness - had to refactor connection strings and configurations
- Two people can request the same laptop, and the admin can also check it out by hand while a request sits in the queue. Availability now gets re-checked at the moment of approval instead of when the request was made
- Stopping the same request landing twice - a double click on submit was enough to do it before I added the pending-request check

## Project Structure

```
AssetFlow/
├── Areas/Identity/      # Login and registration pages
├── Controllers/         # MVC controllers and API endpoints
├── Models/             # Entity models and ViewModels
├── Views/              # Razor views for UI
├── ViewComponents/     # Small reusable pieces (the pending request badge)
├── Data/               # Database context, seeding and migrations
├── wwwroot/            # Static files (CSS, JS, images)
└── appsettings.json    # Configuration
```

## Why This Project Matters

Beyond just being a portfolio piece, I built this to demonstrate that I can:
- Identify real business problems and design solutions
- Build full-stack applications from database to UI
- Create systems that people would actually want to use
- Write clean, maintainable code
- Think about user experience, not just functionality

This system could legitimately be used by a small-to-medium business IT department today. Now that employees can request equipment themselves it is a lot closer to how an IT department actually works.

## Live Demo

[Coming soon]

## Contact

**Moroesi Ramodupi**
- Email: moroesiramodupi@gmail.com
- GitHub: [@MoroesiR](https://github.com/MoroesiR)
- Location: Durban, South Africa
- Currently: Junior Software Developer, open to remote opportunities

## License

This project is open source and available under the MIT License.

---

*Built with C# and way too much coffee ☕*
*Current Version: 2.2.0*
