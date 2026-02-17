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

- **Backend:** ASP.NET Core 8.0, C#
- **Database:** SQL Server (SQLite for dev/testing)
- **ORM:** Entity Framework Core
- **Auth:** ASP.NET Core Identity
- **Frontend:** Razor Pages, Bootstrap 5, custom CSS
- **Charts:** Chart.js for data visualization
- **API Docs:** Swashbuckle/Swagger

## Getting Started

### Prerequisites
- .NET 8.0 SDK
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
- **Email:** admin@gmail.com
- **Password:** Admin@123


## Current Limitations & Known Issues

Right now, this is an admin-only system. The admin does everything - check-in, check-out, maintenance scheduling. 

**Known limitations:**
- No role-based access control yet (working on this - see Future Features)
- No email notifications for overdue items
- Can't reserve equipment in advance
- Maintenance schedule doesn't auto-generate recurring tasks
- No bulk import for adding multiple assets at once

## Future Features

I'm planning to add these next:

### Phase 1: Multi-Role System (In Progress)
- **Regular Users** (employees from different departments)
  - Browse available assets
  - Submit checkout requests
  - View their current checkouts
  - See request status (pending/approved/rejected)
  
- **Admin/IT Department**
  - Receive and approve/reject requests
  - Get notifications when requests come in
  - Assign assets and track who has what
  - All current admin capabilities

### Phase 2: Enhanced Features
- Email notifications (overdue items, approved requests, maintenance reminders)
- Equipment reservation system (book for future dates)
- Recurring maintenance schedules
- Bulk asset import via CSV
- Advanced search and filtering
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

## Project Structure

```
AssetFlow/
├── Controllers/         # MVC controllers and API endpoints
├── Models/             # Entity models and ViewModels
├── Views/              # Razor views for UI
├── Data/               # Database context and migrations
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

This system could legitimately be used by a small-to-medium business IT department today. The multi-role feature I'm adding will make it even more practical.

## Live Demo

[Coming soon - will deploy once the request system is complete]

## Contact

**Moroesi Ramodupi**
- Email: mavundlamoroesi@gmail.com
- GitHub: [@MoroesiR](https://github.com/MoroesiR)
- Location: Durban, South Africa
- Currently: Junior Software Developer, open to remote opportunities

## License

This project is open source and available under the MIT License.

---

*Built with C# and way too much coffee ☕*
*Current Version: 2.1.4*
