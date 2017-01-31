namespace FEC

module EditEmployeePage = // Provides the code backing for the EditEmployeePage view
    open FsXaml
    open System
    open System.Windows
    open System.Data.SQLite
    open System.Windows.Controls
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    // Import the XAML definition of the attendance page's GUI
    type EditEmployeePageBase = XAML<"EditEmployeePage.xaml">

    (* This object represents an individual EditEmployeePage. In WPF, objects are
       used to back the XAML definition of a GUI, so this is the object that gives
       each EditEmployeePage its data context (used for Bindings). The constructor
       takes a SQLite connection to query the database, a Window for changing the
       view, the employee whose information is being edited, and a reference to
       the previous Page so that we can provide a "Back" button. *)
    type EditEmployeePage (conn : SQLiteConnection, mainWindow : Window, employee : Employee, previous : Page) as self =
        inherit EditEmployeePageBase () // Extend the XAML definition

        do self.DataContext <- self // On initialization, attach the DataContext to this object's properties

           // Fill in the FirstName and LastName fields using the employee's info
           self.FirstName.Text <- employee.FirstName
           self.LastName.Text <- employee.LastName

           // Find the matching Job title and set the JobTitle field to display it
           let jobTitles = ["Manager"; "Custodian"; "Attraction Operator"; "Technician"; "Cook"; "Waiter/Waitress"]
           self.JobTitle.SelectedIndex <- List.findIndex (fun j -> j = string_of_job employee.JobTitle) jobTitles
           
           // Split their payment info into the type of pay and the integer value (for efficiency)
           let payType, pay = match employee.Payment with
                              | Wage x -> 0, x   // Index 0 means first option ("Wage")
                              | Salary x -> 1, x // Index 1 means second option ("Salary")
           // Display the PayType and Pay fields
           self.PayType.SelectedIndex <- payType
           self.Pay.Text <- pay_string_of_int pay

        member self.Save = // Command to save the rows to the database and return to the previous page
            let save _ = // Create a function to perform the steps
                // Select the values of the form's fields
                let first = self.FirstName.Text
                let last = self.LastName.Text
                let job = self.JobTitle.Text |> job_of_string
                let pay = match self.PayType.Text with // Using pattern matching, convert the PayType and Pay fields to a Pay object
                          | "Wage" -> Wage (int_of_pay_string self.Pay.Text)
                          | "Salary" -> Salary (int_of_pay_string self.Pay.Text)
                // Update the employee with the proper details
                ignore (updateEmployee conn employee.ID first last job pay)
                let mainView = mainWindow.FindName("MainView") :?> Frame // Get the main view in the window
                mainView.Content <- previous // Set the main view's contents to the previous page
            ClosureCommand save // Convert the "save" function into a Command that WPF can bind to
                
        member self.Cancel = // Command to return to the previous page
            let cancel _ = // Create a function to perform the steps
                let mainView = mainWindow.FindName("MainView") :?> Frame // Get the main view in the window
                mainView.Content <- previous // Set the main view's contents to the previous page
            ClosureCommand cancel // Convert the "cancel" function into a Command that WPF can bind to

    // Convenience function to create a new EditEmployeePage
    let create conn window employee previous = EditEmployeePage (conn, window, employee, previous)
