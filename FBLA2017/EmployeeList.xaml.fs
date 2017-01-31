namespace FEC

module EmployeeList = // Provides the code backing for the EmployeeList view
    open FsXaml
    open System.Windows
    open System.Windows.Controls
    open System.Data.SQLite
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    // Import the XAML definition of the schedule page's GUI
    type EmployeeListBase = XAML<"EmployeeList.xaml">

    (* This object represents an individual employee list. In WPF, objects are used
       to back the XAML definition of a GUI, so this is the object that gives each
       EmployeeList its data context (used for Bindings). The constructor takes a
       SQLite connection to query the database, a Window for changing the view, and
       a reference to the previous Page so that we can provide a "Back" button. *)
    type EmployeeList (conn : SQLiteConnection, mainWindow : Window, previous : Page) as self =
        inherit EmployeeListBase () // Extend the XAML definition

        do self.DataContext <- self // On initialization, attach the DataContext to this object's properties

           // Set up the DataGrid to refresh when the page changes
           let mainView = mainWindow.FindName("MainView") :?> Frame // Get the main window's main view
           let refresh _ = // Create a function to handle the "Navigated" event
               self.Rows <- getEmployees conn // Update the employee data using the database
               self.DataGrid.ItemsSource <- self.Rows // Update the DataGrid's items source to the new data
           mainView.Navigated.Add refresh // When the main view navigates to another page, refresh the list

        let mutable employees = getEmployees conn // A mutable collection of the employees to display

        // Corresponds to the "employees" value, but as a property that can be bound by WPF
        member self.Rows with get () = employees and set value = employees <- value

        // Gets the list of Employees from the Rows
        member self.GetEmployees = self.Rows

        member self.AddEmployee = // Command to add a new employee using AddEmployeePage
            let add _ = // Create a function to perform the steps
                // Get the main view in the window
                let mainView = mainWindow.FindName("MainView") :?> Frame
                // Set the main view's contents to a new AddEmployeePage
                mainView.Content <- AddEmployeePage.create conn mainWindow self
            ClosureCommand add // Convert the "add" function into a Command that WPF can bind to

        member self.Done = // Command to return to the previous page
            let ``done`` _ = // Create a function to perform the steps
                // Get the main view in the window
                let mainView = mainWindow.FindName("MainView") :?> Frame
                // Set the main view's contents to the previous page
                mainView.Content <- previous
            ClosureCommand ``done`` // Convert the "add" function into a Command that WPF can bind to

        member self.EditEmployee = // Command to edit an employee using EditEmployeePage
            let edit (param : obj) = // Create a function, taking an employee ID, to perform the steps
                let id = param :?> int // Coerce the parameter (ID) to int
                // Search all employees in the list for an employee with the matching ID
                let employee = query {
                    for employee in self.Rows do
                        where (id = employee.ID)
                        select employee
                        exactlyOne }
                // Get the main view in the window
                let mainView = mainWindow.FindName("MainView") :?> Frame
                // Set the main view's contents to a new EditEmployeePage for the employee found
                mainView.Content <- EditEmployeePage.create conn mainWindow employee self
            ClosureCommand edit // Convert the "edit" function into a Command that WPF can bind to

        member self.EditSchedule = // Command to edit an employee's schedule using SchedulePage
            let edit (param : obj) = // Create a function, taking an employee ID, to perform the steps
                let id = param :?> int // Coerce the parameter (ID) to int
                // Search all employees in the list for an employee with the matching ID
                let schedule = query {
                    for employee in employees do
                        where (id = employee.ID)
                        select employee.Schedule
                        exactlyOne }
                // Get the main view in the window
                let mainView = mainWindow.FindName("MainView") :?> Frame
                // Set the main view's contents to a new SchedulePage for the employee found
                mainView.Content <- SchedulePage.create conn mainWindow id schedule self
            ClosureCommand edit // Convert the "edit" function into a Command that WPF can bind to

        member self.DeleteEmployee = // Command to delete an employee from the database
            let delete (param : obj) = // Create a function, taking an employee ID, to perform the steps
                let id = param :?> int // Coerce the parameter (ID) to int
                deleteEmployee conn id // Delete the employee by ID

                // Force the data to refresh
                self.Rows <- getEmployees conn // Update the employee data using the database
                self.DataGrid.ItemsSource <- self.Rows // Update the DataGrid's items source to the new data
            ClosureCommand delete // Convert the "delete" function into a Command that WPF can bind to

    // Convenience function to create a new EmployeeList
    let create conn window previous = EmployeeList (conn, window, previous)
