namespace FEC

module AddEmployeePage = // Provides the code backing for the AddEmployeePage view
    open FsXaml
    open System
    open System.Windows
    open System.Data.SQLite
    open System.Windows.Controls
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    // Import the XAML definition of the attendance page's GUI
    type AddEmployeePageBase = XAML<"AddEmployeePage.xaml">

    (* This object represents an individual AddEmployeePage. In WPF, objects are
       used to back the XAML definition of a GUI, so this is the object that gives
       each AddEmployeePage its data context (used for Bindings). The constructor
       takes a SQLite connection to query the database, a Window for changing the
       view, and a reference to the previous Page so that we can provide a "Back"
       button. *)
    type AddEmployeePage (conn : SQLiteConnection, mainWindow : Window, previous : Page) as self =
        inherit AddEmployeePageBase () // Extend the XAML definition

        do self.DataContext <- self // On initialization, attach the DataContext to this object's properties
        
        member self.Save = // Command to save the rows to the database and return to the previous page
            let save _ = // Create a function to perform the steps
                // Select the values of the form's fields
                let first = self.FirstName.Text
                let last = self.LastName.Text
                let job = self.JobTitle.Text |> job_of_string
                let pay = match self.PayType.Text with // Using pattern matching, convert the PayType and Pay fields to a Pay object
                          | "Wage" -> Wage (int_of_pay_string self.Pay.Text)
                          | "Salary" -> Salary (int_of_pay_string self.Pay.Text)
                // Add the employee to the database with a blank schedule, then dispose of the result
                ignore (addEmployee conn first last job pay <| blankSchedule ())
                let mainView = mainWindow.FindName("MainView") :?> Frame // Get the main view in the window
                mainView.Content <- previous // Set the main view's contents to the previous page
            ClosureCommand save // Convert the "save" function into a Command that WPF can bind to

        member self.Cancel = // Command to return to the previous page
            let cancel _ = // Create a function to perform the steps
                let mainView = mainWindow.FindName("MainView") :?> Frame // Get the main view in the window
                mainView.Content <- previous // Set the main view's contents to the previous page
            ClosureCommand cancel // Convert the "cancel" function into a Command that WPF can bind to

    // Convenience function to create a new AddEmployeePage
    let create conn window previous = AddEmployeePage (conn, window, previous)
