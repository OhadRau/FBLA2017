namespace FEC

module Dashboard = // Provides the code backing for the Dashboard view
    open FsXaml
    open System.Windows
    open System.Data.SQLite
    open System.Windows.Controls
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    // Import the XAML definition of the schedule page's GUI
    type DashboardBase = XAML<"Dashboard.xaml">

    (* This object represents an individual dashboard page. In WPF, objects are
       used to back the XAML definition of a GUI, so this is the object that gives
       each Dashboard its data context (used for Bindings). The constructor takes
       a SQLite connection to query the database and a Window for changing the view. *)
    type Dashboard (conn : SQLiteConnection, mainWindow : Window) as self =
        inherit DashboardBase () // Extend the XAML definition

        do self.DataContext <- self // On initialization, attach the DataContext to this object's properties

           let mainView = mainWindow.FindName("MainView") :?> Frame // Get the main window's main view
           let refresh (u : Control) _ = // Create a function to handle the "Navigated" event for a given Control 
               // Force refresh by "changing" the data context to itself
               let context = u.DataContext
               u.DataContext <- null
               u.DataContext <- context
           // With the controls for the employee count, employee expenses, daily attendance, and hourly attendance
           ([self.EmployeeCountControl; self.EmployeeExpensesControl; self.DailyAttendanceControl; self.HourlyAttendanceControl] : Control list)
           |> List.map refresh // Create a refresh function for each control
           |> List.iter mainView.Navigated.Add // And add each refresh function to the "Navigated" event of the main view

        member self.AddEmployee = // Command to go to an AddEmployeePage
            let add _ =
                // Get the main window's main view
                let mainView = mainWindow.FindName("MainView") :?> Frame
                // Set the main view's contents to a new AddEmployeePage for this week
                mainView.Content <- AddEmployeePage.create conn mainWindow self
            ClosureCommand add // Convert the "add" function into a Command that WPF can bind to

        member self.ViewEmployees = // Command to go to an EmployeeList
            let view _ =
                // Get the main window's main view
                let mainView = mainWindow.FindName("MainView") :?> Frame
                // Set the main view's contents to a new EmployeeList for this week
                mainView.Content <- EmployeeList.create conn mainWindow self
            ClosureCommand view // Convert the "view" function into a Command that WPF can bind to

        member self.ViewAttendance = // Command to go to an AttendancePage
            let view _ =
                // Get the attendance for the current week
                let attendance = getAttendance conn System.DateTime.Now
                // Get the main window's main view
                let mainView = mainWindow.FindName("MainView") :?> Frame
                // Set the main view's contents to a new AttendancePage for this week
                mainView.Content <- AttendancePage.create conn mainWindow attendance (getWeek ()) self
            ClosureCommand view // Convert the "view" function into a Command that WPF can bind to

        member self.PeakDays = // Command to show the Peak Days report
            let graph _ = Reports.peakDays conn // Create a function to generate the report using the database
            ClosureCommand graph // Convert the "graph" function into a Command that WPF can bind to

        member self.PeakHours = // Command to show the Peak Hours report
            let graph _ = Reports.peakHours conn // Create a function to generate the report using the database
            ClosureCommand graph // Convert the "graph" function into a Command that WPF can bind to

        member self.EmployeeCount // Dynamic data source giving the number of employees
            with get () =
                (getEmployees conn).Count // Count the number of employees in the database

        member self.EmployeeExpenses // Dynamic data source giving the amount of money spent on employee pay per week
            with get () =
                let employees = getEmployees conn // Get all the employees in the database
                let addDays week = // Create a function to sum the number of days worked per each hour in a week
                    let asInt = function true -> 1 | false -> 0 // If they're working, count it as 1 hour
                    // Sum up the entire hour for every day of the week worked during that time-slot
                    asInt week.Sunday + asInt week.Monday + asInt week.Tuesday + asInt week.Wednesday + asInt week.Thursday + asInt week.Friday + asInt week.Saturday
                // Add up the amount paid within a one week span for every employee found
                query { for e in employees do
                        sumBy (match e.Payment with
                               // If the employee is paid by the hour, sum up their hours worked and multiply by their wage
                               | Wage w -> w * (Seq.map addDays e.Schedule |> Seq.sum)
                               // If the employee is paid a fixed yearly salary, divide it by 52 (since 1 year = 52 weeks)
                               | Salary s -> s / 52 )}
        
        member self.DailyAttendance // Dynamic data source giving the number of visitors today
            with get () =
                // Get the attendance information for the current week
                let attendance = getAttendance conn System.DateTime.Now
                // Create a function to yield the attendance number for the current day of the week
                let getToday hour = match System.DateTime.Now.DayOfWeek with
                    | System.DayOfWeek.Sunday -> hour.Sunday
                    | System.DayOfWeek.Monday -> hour.Monday
                    | System.DayOfWeek.Tuesday -> hour.Tuesday
                    | System.DayOfWeek.Wednesday -> hour.Wednesday
                    | System.DayOfWeek.Thursday -> hour.Thursday
                    | System.DayOfWeek.Friday -> hour.Friday
                    | System.DayOfWeek.Saturday -> hour.Saturday
                // Get each hour's attendance counts for today, then sum them up to a single number
                Array.map getToday attendance |> Array.sum

        member self.HourlyAttendance // Dynamic data source giving the number of visitors this hour
            with get () =
                // Get the attendance information for the current week
                let attendance = getAttendance conn System.DateTime.Now
                // Find a reference to the current hour within the attendance information
                let hour = attendance.[System.DateTime.Now.Hour]
                // Return the number of attendees for that hour today
                match System.DateTime.Now.DayOfWeek with
                    | System.DayOfWeek.Sunday -> hour.Sunday
                    | System.DayOfWeek.Monday -> hour.Monday
                    | System.DayOfWeek.Tuesday -> hour.Tuesday
                    | System.DayOfWeek.Wednesday -> hour.Wednesday
                    | System.DayOfWeek.Thursday -> hour.Thursday
                    | System.DayOfWeek.Friday -> hour.Friday
                    | System.DayOfWeek.Saturday -> hour.Saturday
            and set count =
                // Get the attendance information for the current week
                let attendance = getAttendance conn System.DateTime.Now
                // Find a reference to the current hour within the attendance information
                let hour = attendance.[System.DateTime.Now.Hour]
                // Update the number of attendees for that hour today to the value of "count"
                match System.DateTime.Now.DayOfWeek with
                    | System.DayOfWeek.Sunday -> hour.Sunday <- count
                    | System.DayOfWeek.Monday -> hour.Monday <- count
                    | System.DayOfWeek.Tuesday -> hour.Tuesday <- count
                    | System.DayOfWeek.Wednesday -> hour.Wednesday <- count
                    | System.DayOfWeek.Thursday -> hour.Thursday <- count
                    | System.DayOfWeek.Friday -> hour.Friday <- count
                    | System.DayOfWeek.Saturday -> hour.Saturday <- count
                // Save the now-changed attendance information to the database
                saveAttendance conn System.DateTime.Now attendance

    // Convenience function to create a new Dashboard
    let create conn window = Dashboard (conn, window)
