namespace FEC

module SchedulePage = // Provides the code backing for the SchedulePage view
    open FsXaml
    open System.Windows
    open System.Data.SQLite
    open System.Windows.Controls
    open System.Collections.ObjectModel

    open FEC.Data
    open FEC.Common

    (* This type represents a row used in the attendance DataGrid. Specifically,
       since WPF allows for only a single data source for DataGrids, we must combine
       the data being displayed. This data is the hour label, followed by each hour's
       schedule. *)
    type MergedData = {
        Label : string
        Hours : Hour<bool>
    }

    // Import the XAML definition of the schedule page's GUI
    type SchedulePageBase = XAML<"SchedulePage.xaml">

    (* This object represents an individual schedule page. In WPF, objects are
       used to back the XAML definition of a GUI, so this is the object that gives
       each SchedulePage its data context (used for Bindings). The constructor takes
       a SQLite connection to query the database, a Window for changing the view,
       ID for the employee whose schedule is being edited, that employee's schedule,
       and a reference to the previous Page so that we can provide a "Back" button. *)
    type SchedulePage (conn : SQLiteConnection, mainWindow : Window, id : int, schedule : Hour<bool>[], previous : Page) as self =
        inherit SchedulePageBase () // Extend the XAML definition

        do self.DataContext <- self // On initialization, attach the DataContext to this object's properties

        let mutable merged = // A mutable collection of the MergedData/rows to display
            let labels = // An array of labels for each hour ("12:00 AM", "1:00 AM", ...)
                let hour_of_int i = // Function to convert an int (0-23) into an hour label
                    (* If the hour is divisible by 12, then the hour is called "12"
                       (i.e. 12 AM and 12 PM). Otherwise, it keeps the same number,
                       module 12 (e.g. hour #2 = 2 AM, hour #14 = 2 PM). *)
                    let hour = if i % 12 = 0 then 12 else i % 12
                    let period = if i < 12 then "AM" else "PM" // Before noon is "AM", after is "PM"
                    sprintf "%i:00 %s" hour period // Format this information into a time string
                Array.map hour_of_int [|0..23|] // Turn every int from 0 to 23 into an hour label

            // Merge the two sets of data (labels and schedule hours) into a single array
            Array.map2 (fun l h -> { Label=l; Hours=h }) labels schedule

        member val Rows = merged // Corresponds to the "merged" value, but as a property that can be bound by WPF

        // Gets the schedule from the Rows of the DataGrid by taking only the "Hours" of each row
        member self.GetSchedule = self.Rows |> Array.map (fun s -> s.Hours)

        member self.Save = // Command to save the rows to the database and return to the previous page
            let save _ = // Create a function to perform the steps
                saveSchedule conn id self.GetSchedule // Call saveSchedule with the current Schedule data
                let mainView = mainWindow.FindName("MainView") :?> Frame // Get the main view in the window
                mainView.Content <- previous // Set the main view's contents to the previous page
            ClosureCommand save // Convert the "save" function into a Command that WPF can bind to

        member self.Cancel = // Command to return to the previous page
            let cancel _ = // Create a function to perform the steps
                let mainView = mainWindow.FindName("MainView") :?> Frame // Get the main view in the window
                mainView.Content <- previous // Set the main view's contents to the previous page
            ClosureCommand cancel // Convert the "cancel" function into a Command that WPF can bind to

    // Convenience function to create a new SchedulePage
    let create conn window id schedule previous = SchedulePage (conn, window, id, schedule, previous)
