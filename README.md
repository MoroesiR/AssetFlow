# AssetFlow - IT Asset Management System

## Project Overview
AssetFlow is a comprehensive web-based system for tracking and managing IT equipment within organizations. Built with ASP.NET Core MVC, this application helps companies manage hardware inventory, track checkouts, schedule maintenance, and generate reports.

While developed as a portfolio project to showcase my full-stack ASP.NET Core skills, every feature was designed with real organizational needs in mind. The system addresses common business challenges around asset tracking and demonstrates my ability to build professional-grade software solutions.

## Project Background

### The Problem
Many organizations struggle with tracking IT equipment, managing employee checkouts, and scheduling preventive maintenance. Manual processes and spreadsheets often lead to errors, lost assets, and operational inefficiencies.

### The Solution
AssetFlow provides a professional web-based solution for comprehensive asset management. This project represents my journey in learning ASP.NET Core while focusing on building something that could actually be used in a business environment.

### What This Demonstrates
Beyond technical skills, this project shows my ability to:
- Identify inefficiencies in current business processes
- Design intuitive solutions that users will actually adopt
- Implement robust systems that handle real business logic
- Create professional documentation and user interfaces

## Features

### Core Functionality
- Complete asset lifecycle management (add, update, track, retire)
- Employee checkout system with automated status tracking
- Maintenance scheduling and tracking
- Comprehensive reporting and analytics
- User authentication and role management

### Asset Management
- Full CRUD operations for IT assets
- Categorization (Laptops, Servers, Networking, AV Equipment, etc.)
- Serial number tracking with duplicate prevention
- Purchase details including price, date, vendor, and warranty
- Location tracking and status management

### Checkout System
- Employee checkout tracking with department information
- Automated status updates (Available → Checked Out → Available)
- Expected return dates with overdue indicators
- Condition notes and maintenance flagging during returns
- Complete checkout history for audit purposes

### Maintenance Management
- Scheduled maintenance with due date tracking
- Condition assessment during check-in
- Maintenance notes and repair history
- Visual indicators for overdue maintenance
- Quick status toggling between maintenance and available states

### Reporting & Analytics
- Asset value reports by category with data visualization
- Checkout history with customizable date ranges
- Maintenance schedules with color-coded urgency indicators
- CSV export functionality for external analysis
- Print-friendly report layouts

### API & Integration
- RESTful API endpoints for programmatic access
- Swagger/OpenAPI documentation with interactive testing
- JSON responses for integration with other systems
- Search and filter capabilities via API

### User Experience
- Responsive design that works on desktop and mobile devices
- Clean, professional interface with custom CSS styling
- Real-time status updates and intuitive workflows
- Efficient navigation and bulk operation support

## Technology Stack

### Backend
- ASP.NET Core 8.0 - Modern web framework
- Entity Framework Core - Object-relational mapping
- SQL Server - Primary database (with SQLite for development)
- ASP.NET Core Identity - Authentication and authorization
- Swashbuckle/Swagger - API documentation

### Frontend
- Razor Pages with C# server-side rendering
- Bootstrap 5 - Responsive layout foundation
- Custom CSS - Professional styling and theming
- Chart.js - Data visualization for reports

### Development Tools
- Visual Studio 2022 - Primary development environment
- Git - Version control
- SQL Server Management Studio - Database management
- Package Manager Console - NuGet package management

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- SQL Server (or SQLite for development)

### Installation Steps
1. Clone the repository
   ```bash
   git clone https://github.com/MoroesiR/assetflow.git
   cd assetflow
Set up the database

bash
# Apply migrations
dotnet ef database update
Configure the connection string

Update appsettings.json with your database connection

SQLite is pre-configured for development use

Run the application

bash
dotnet run
# Or run from Visual Studio with F5
Access the application

Open https://localhost:5001 in your browser

Default demo credentials:

Email: admin@gmail.com

Password: Admin@123

### First-Time Setup
Register a new user account or use demo credentials

Add your first asset using the "Add Asset" button

Explore the dashboard for system overview

Test the checkout process with sample data

Generate reports to see data visualization in action

### What I Learned

## Technical Skills Developed
Full-stack development with ASP.NET Core MVC

Database design and Entity Framework migrations

REST API design and documentation with Swagger

Authentication implementation with ASP.NET Identity

Responsive UI design with Bootstrap and custom CSS

Reporting and data visualization implementation

Development Process Insights
Feature prioritization for minimum viable product

User workflow design based on real business needs

Error handling and user feedback implementation

Code organization for maintainability

Importance of documentation throughout development

## Challenges Overcome
Complex state management for asset lifecycle tracking

Data validation across multiple related fields

User interface design for non-technical users

Database relationship design for audit trails

API design considerations for potential integration

### About the Developer
Background
I'm a junior software developer from South Africa passionate about building practical solutions with modern technologies. This project represents my journey in mastering ASP.NET Core and full-stack web development.

### Skills Demonstrated
ASP.NET Core MVC development

Database design with Entity Framework

REST API implementation

Professional UI/UX design

Problem-solving and system design

### Contact
Email: mavundlamoroesi@gmail.com

GitHub: https://github.com/MoroesiR
