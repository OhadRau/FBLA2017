namespace FEC

module Data = // Provides data types and database functions
    open System
    open System.IO
    open System.Linq
    open System.Data
    open System.Data.SQLite
    open System.Runtime.Serialization
    open System.Runtime.Serialization.Formatters.Binary

    (* This data structure serves as the in-memory layout
       of all weekly data (employee schedules and customer
       attendance). The layout chosen seems strange (since
       it stores only 1 hour for every day of the week),
       but this was chosen because it makes for more efficient
       SQL queries and makes WPF data bindings quicker. While
       the "array of struct" pattern is generally slower than
       "struct of array" on x86 processers, due to the VM in
       which F# code is executed there is very minimal performance
       loss, which can be gained back by the fast data bindings. *) 
    type Hour<'a> = { // 24-hour units for every day
        mutable Sunday    : 'a
        mutable Monday    : 'a
        mutable Tuesday   : 'a
        mutable Wednesday : 'a
        mutable Thursday  : 'a
        mutable Friday    : 'a
        mutable Saturday  : 'a
    }

    (* Initializes an array of 24 Hour<bool>s, which
       are used to represent schedules on a 24-hour day.
       The reason this is a function/not a constant is
       because it is a mutable reference. If this value
       were a constant, every schedule would overwrite
       all schedules loaded into memory when edited. *)
    let blankSchedule () = Array.init 24 <| fun _ -> {
        Sunday    = false
        Monday    = false
        Tuesday   = false
        Wednesday = false
        Thursday  = false
        Friday    = false
        Saturday  = false
    }

    (* Initializes an array of 24 Hour<int>s, which
       are used to represent attendance on a 24-hour day.
       Otherwise similar to blankSchedule *)
    let blankAttendance () = Array.init 24 <| fun _ -> {
        Sunday    = 0
        Monday    = 0
        Tuesday   = 0
        Wednesday = 0
        Thursday  = 0
        Friday    = 0
        Saturday  = 0
    }

    // An enumeration of the different job titles available
    type Job =
        | Manager
        | Custodian
        | AttractionOperator
        | Technician
        | Cook
        | Waiter

    // Use pattern matching to format each Job as a string
    let string_of_job = function
        | Manager -> "Manager"
        | Custodian -> "Custodian"
        | AttractionOperator -> "Attraction Operator"
        | Technician -> "Technician"
        | Cook -> "Cook"
        | Waiter -> "Waiter/Waitress"

    // Use pattern matching to convert a string back to a Job
    let job_of_string = function
        | "Manager" -> Manager
        | "Custodian" -> Custodian
        | "Attraction Operator" -> AttractionOperator
        | "Technician" -> Technician
        | "Cook" -> Cook
        | "Waiter/Waitress" -> Waiter
        | _ -> failwith "Unknown job type" // Any other string is an error

    (* Represents an employee's payment information.
       All employees are paid either as a salary
       (annually) or as a wage (weekly). Ints are used
       instead of floats to represent currency, as
       floating-point math can lead to inaccurate results.
       Each int is multiplied by 100 (i.e. $11.55 is 1155). *)
    type Pay = Wage of int | Salary of int
    
    // Format an integer as a payment/currency string (i.e. 1155 -> $11.55)
    let pay_string_of_int x =
        let dollars = x / 100 // Use integer division by 100 to get the dollars
        let cents = x % 100 // Modulo 100 "subtracts" the dollars, leaving only cents
        sprintf "$%d.%02d" dollars cents // Format with exactly 2 digits for the cents

    // Use pattern matching to format an employee's payment info as a string
    let string_of_pay = function
        | Wage x -> // If the employee is paid in a wage
          pay_string_of_int x + " per hour" // Add "per hour" to the currency value
        | Salary x -> // If the employee is paid in a salary
          pay_string_of_int x + " per year" // Add "per year" to the currency value

    // Convert a string into an amount of currency
    let int_of_pay_string (text : string) =
        let filtered = String.Join ("", text.Split [|'$'; ','|]) // Ignore dollar sign and commas between digits
        match filtered.Split [|'.'|] with // Split the string on the decimal point to get dollars and cents
        | [|dollars|] -> 100 * Convert.ToInt32 dollars // If they didn't fill in cents, just use the dollar value
        | [|dollars; cents|] -> 100 * Convert.ToInt32 dollars + Convert.ToInt32 cents // Otherwise, use both dollars and cents
        | _ -> 0 // If the value is not properly formatted, it represents no money

    (* This data structure represents an employee's information as
       it is stored in the database. The ID value is a reference
       into the database's primary key, which allows for O(1) access
       to the value in the database. *)
    type Employee = {
        mutable ID : int
        mutable FirstName : string
        mutable LastName : string
        mutable JobTitle : Job
        mutable Payment : Pay
        mutable Schedule : Hour<bool>[] // 24x
    }

    // Convert any value into a stream of binary data (as it is laid out in memory)
    let serialize<'a> (x :'a) =
        // Initialize a BinaryFormatter, which allows us to retrieve binary data
        let binFormatter = BinaryFormatter ()
        // Access a MemoryStream as a resource
        use stream = MemoryStream ()
        // Retrieve the value's binary data and save it to the memory stream
        binFormatter.Serialize (stream, x)
        // Return the memory stream as an array of bytes
        stream.ToArray ()

    // Load a value into memory from a serialized array of bytes
    let deserialize<'a> (arr : byte[]) =
        // Initialize a BinaryFormatter, which allows us to retrieve binary data
        let binFormatter = BinaryFormatter ()
        // Access a MemoryStream as a resource and point it to the array of bytes
        use stream = MemoryStream arr
        // Retrieve the value's data from the stream and coerce it to the correct type
        binFormatter.Deserialize stream :?> 'a

    // Creates a new SQLite database file or connects to an existing one, using dbFile as the path to the database
    let createOrOpen dbFile =
        let mutable init = false // Should we initialize it with CREATE clauses?
        if not (File.Exists dbFile) then
            SQLiteConnection.CreateFile dbFile // Create the file with the correct attributes for the database
            init <- true // Since the file was just created, yes
        // Format a .NET-style connection string
        let conn_string = sprintf "DataSource=%s;Version=3;" dbFile
        // Connect to the database using that string (don't directly open it as a file!)
        let conn = SQLiteConnection (conn_string)
        conn.Open () // Now unlock it so that we can begin sending queries
        if init then // If the database hasn't yet been initialized
            // Create a table of employee data
            let create_employees = "CREATE TABLE employees (first TEXT NOT NULL,
                                                            last TEXT NOT NULL,
                                                            job TEXT NOT NULL,
                                                            payType TEXT NOT NULL,
                                                            pay INTEGER NOT NULL,
                                                            schedule BLOB NOT NULL)"
            // Create a table of attendance data
            let create_attendance = "CREATE TABLE attendance (sunday DATETIME UNIQUE,
                                                              attendance BLOB NOT NULL)"
            // Run both of these queries on the connection and dispose of the result
            [ SQLiteCommand (create_employees, conn)
            ; SQLiteCommand (create_attendance, conn)
            ] |> List.map (fun c -> c.ExecuteNonQuery ()) |> ignore
        conn // Return the connection to the database so it can be reused

    // Add a new employee to the database and assign them an ID
    let addEmployee conn firstName lastName jobTitle payment schedule =
        // Split their payment info into the type of pay and the integer value (for efficiency)
        let (payType, pay) = match payment with
                             | Salary x -> ("Salary", x)
                             | Wage x -> ("Wage", x)

        // Convert the schedule into a byte array that can be stored in the database
        let blob = serialize<Hour<bool>[]> schedule

        // Bind each parameter to a short name within the query
        let fParam = SQLiteParameter ("@f", DbType.String, Value=firstName)
        let lParam = SQLiteParameter ("@l", DbType.String, Value=lastName)
        let jParam = SQLiteParameter ("@j", DbType.String, Value=string_of_job jobTitle)
        let tParam = SQLiteParameter ("@t", DbType.String, Value=payType)
        let pParam = SQLiteParameter ("@p", DbType.Int32,  Value=pay)
        let sParam = SQLiteParameter ("@s", DbType.Binary, Value=blob)

        // Insert every parameter to the employees table as a new employee
        let query = "INSERT INTO employees VALUES (@f, @l, @j, @t, @p, @s)"
        let cmd = SQLiteCommand (query, conn)
        List.map cmd.Parameters.Add [fParam; lParam; jParam; tParam; pParam; sParam] |> ignore
        cmd.ExecuteNonQuery () |> ignore // Execute the query and dispose of the result

        // Find the ID of the last row inserted to the database (our new employee)
        let query = "SELECT last_insert_rowid()"
        let cmd = SQLiteCommand (query, conn)
        let id = cmd.ExecuteScalar () // Execute the query and save the result

        // Return the employee info as a value of type Employee
        { ID = Convert.ToInt32 id
          FirstName = firstName
          LastName = lastName
          JobTitle = jobTitle
          Payment = payment
          Schedule = schedule }

    // Update an existing employee's basic information
    let updateEmployee conn id firstName lastName jobTitle payment =
        // Split their payment info into the type of pay and the integer value (for efficiency)
        let (payType, pay) = match payment with
                             | Salary x -> ("Salary", x)
                             | Wage x -> ("Wage", x)

        // Bind each parameter to a short name within the query
        let fParam = SQLiteParameter ("@f", DbType.String, Value=firstName)
        let lParam = SQLiteParameter ("@l", DbType.String, Value=lastName)
        let jParam = SQLiteParameter ("@j", DbType.String, Value=string_of_job jobTitle)
        let tParam = SQLiteParameter ("@t", DbType.String, Value=payType)
        let pParam = SQLiteParameter ("@p", DbType.Int32,  Value=pay)
        let iParam = SQLiteParameter ("@i", DbType.Int32,  Value=id)

        // Update the employee with the correct ID using the new information
        let query = "UPDATE employees SET first=@f, last=@l, job=@j, payType=@t, pay=@p WHERE rowid=@i"
        let cmd = SQLiteCommand (query, conn)
        List.map cmd.Parameters.Add [fParam; lParam; jParam; tParam; pParam; iParam] |> ignore
        cmd.ExecuteNonQuery () |> ignore // Execute the query and dispose of the result

    // Update an existing employee's schedule
    let saveSchedule conn id schedule =
        // Convert the schedule into a byte array that can be stored in the database
        let blob = serialize<Hour<bool>[]> schedule

        // Bind each parameter to a short name within the query
        let sParam = SQLiteParameter ("@s", DbType.Binary, Value=blob)
        let iParam = SQLiteParameter ("@i", DbType.Int32, Value=id)

        // Update the employee with the correct ID using the new schedule
        let query = "UPDATE employees SET schedule=@s WHERE rowid=@i"
        let cmd = SQLiteCommand (query, conn)
        List.map cmd.Parameters.Add [sParam; iParam] |> ignore
        cmd.ExecuteNonQuery () |> ignore // Execute the query and dispose of the result

    // Delete an employee from the active roster
    let deleteEmployee conn id =
        // Bind the employee's ID to the parameter "i" in the query
        let iParam = SQLiteParameter ("@i", DbType.Int32, Value=id)

        // Delete the employee with the matching ID
        let query = "DELETE FROM employees WHERE rowid=@i"
        let cmd = SQLiteCommand (query, conn)
        cmd.Parameters.Add iParam |> ignore
        cmd.ExecuteNonQuery () |> ignore // Execute the query and dispose of the result
       
    // Get a full list of employees in the database
    let getEmployees conn =
        // Get every field/column for every single employee stored
        let query = "SELECT rowid, first, last, job, payType, pay, schedule FROM employees"
        let cmd = SQLiteCommand (query, conn)
        let employees = cmd.ExecuteReader () // Execute the query and start reading the results
    
        // Create an empty, mutable array-list of employees to return
        let mutable result = ResizeArray<Employee> ()
        // While there are still employees left, progress through the results
        while employees.Read () do
            // Determine the type of pay and the currency integer for the employee
            let isSalary = (employees.["payType"] :?> string) = "Salary"
            let pay = Convert.ToInt32 employees.["pay"]
            // Add a new value to represent the employee to the array-list
            result.Add { ID = Convert.ToInt32 employees.["rowid"]
                       ; FirstName = employees.["first"] :?> string
                       ; LastName = employees.["last"] :?> string
                       ; JobTitle = employees.["job"] :?> string |> job_of_string
                       ; Payment = if isSalary then Salary pay else Wage pay
                       // Load the serialized schedule back into memory and use it as the schedule field
                       ; Schedule = deserialize<Hour<bool>[]> (employees.["schedule"] :?> byte[])
                       }
        result // Return the array-list of employees

    (* Convert any DateTime object to a DateTime object representing that week as a whole.
       This is done by storing each week as the first second of the first day of the week,
       i.e. 12:00:00 AM on Sunday of that week. *)
    let week_of_datetime (week : System.DateTime) =
        let week' = week.AddDays(-(Convert.ToDouble week.DayOfWeek)) // Move to that Sunday
        DateTime (week'.Year, week'.Month, week'.Day) // Only save the date aspect (no hours/minutes/seconds)

    (* Convert any DateTime object to a DateTime object representing that hour as a whole.
       This is done by storing each hour as the first second of that hour, i.e X:00:00. *)
    let hour_of_datetime (day : System.DateTime) =
        // Save the date aspect and add in the hour, then set the minutes and seconds to 0
        DateTime (day.Year, day.Month, day.Day, day.Hour, 0, 0)

    // Get the DateTime object representing the current week
    let getWeek () = week_of_datetime System.DateTime.Now

    // Get the attendance for a specific week
    let getAttendance conn datetime =
        // Normalize the DateTime to the Sunday of that week
        let normalized = week_of_datetime datetime
        // Bind the normalized DateTime to the parameter "s" in the query
        let sParam = SQLiteParameter ("@s", DbType.DateTime, Value=normalized)
        
        // Get the attendance object for the matching sunday
        let query = "SELECT attendance FROM attendance WHERE sunday = @s"
        let cmd = SQLiteCommand (query, conn)
        cmd.Parameters.Add sParam |> ignore
        let bytes = cmd.ExecuteScalar () :?> byte[] // Read the result as an array of bytes
        if bytes = null // The object may be null (if it's the first time using it that week)
            then blankAttendance () // If it is null, then just use a blank attendance object
            else deserialize<Hour<int>[]> bytes // Otherwise deserialize it and load it into memory

    // Get a list of all attendance for every week
    let getAllAttendance conn =
        let query = "SELECT * FROM attendance" // Select every single week
        let cmd = SQLiteCommand (query, conn)

        let weeks = cmd.ExecuteReader () // Execute the query and start reading the results

        // Create an empty, mutable array-list of attendance weeks to return
        let mutable result = ResizeArray<Hour<int>[]> ()
        // While there are still weeks left, progress through the results
        while weeks.Read () do
            // Deserialize that week's attendance object and add it to the list of results
            result.Add <| deserialize<Hour<int>[]> (weeks.["attendance"] :?> byte[])
        result // Return the array-list

    let saveAttendance conn datetime attendance =
        // Normalize the DateTime to the Sunday of that week and bind it to parameter "s" in the query
        let normalized = week_of_datetime datetime
        let sParam = SQLiteParameter ("@s", DbType.DateTime, Value=normalized)

        // Serialize the attendance object to a byte array and bind it to parameter "a" in the query
        let blob = serialize<Hour<int>[]> attendance
        let aParam = SQLiteParameter ("@a", DbType.Binary, Value=blob)

        // Update that week to contain the new attendance info (add a row for that week if one doesn't exist)
        let query = "INSERT OR REPLACE INTO attendance (sunday, attendance) VALUES (@s, @a)"
        let cmd = SQLiteCommand (query, conn)
        List.map cmd.Parameters.Add [sParam; aParam] |> ignore
        cmd.ExecuteNonQuery () |> ignore // Execute the query and dispose of the result
