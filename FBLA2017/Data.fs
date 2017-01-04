﻿namespace FEC

module Data =
    open System
    open System.IO
    open System.Linq
    open System.Data
    open System.Data.SQLite
    open System.Runtime.Serialization
    open System.Runtime.Serialization.Formatters.Binary

    type Hour = { // 24-hour units for every day
        mutable Sunday    : bool
        mutable Monday    : bool
        mutable Tuesday   : bool
        mutable Wednesday : bool
        mutable Thursday  : bool
        mutable Friday    : bool
        mutable Saturday  : bool
    }

    let blankSchedule () = Array.init 24 <| fun _ -> {
        Sunday    = false
        Monday    = false
        Tuesday   = false
        Wednesday = false
        Thursday  = false
        Friday    = false
        Saturday  = false
    }

    type Job =
        | Manager
        | Custodian
        | AttractionOperator
        | Technician
        | Cook
        | Waiter

    let string_of_job = function
        | Manager -> "Manager"
        | Custodian -> "Custodian"
        | AttractionOperator -> "Attraction Operator"
        | Technician -> "Technician"
        | Cook -> "Cook"
        | Waiter -> "Waiter/Waitress"

    let job_of_string = function
        | "Manager" -> Manager
        | "Custodian" -> Custodian
        | "Attraction Operator" -> AttractionOperator
        | "Technician" -> Technician
        | "Cook" -> Cook
        | "Waiter/Waitress" -> Waiter
        | _ -> failwith "Unknown job type"

    type Pay = Wage of int | Salary of int

    let string_of_pay = function
        | Wage x ->
          let dollars = x / 100
          let cents = x % 100
          sprintf "$%d.%02d per hour" dollars cents
        | Salary x ->
          let dollars = x / 100
          let cents = x % 100
          sprintf "$%d.%02d per year" dollars cents

    let pay_string_of_int x =
        let dollars = x / 100
        let cents = x % 100
        sprintf "$%d.%d" dollars cents

    let int_of_pay_string (text : string) =
        let filtered = String.Join ("", text.Split [|'$'; ','|])
        match filtered.Split [|'.'|] with
        | [|dollars|] -> 100 * Convert.ToInt32 dollars
        | [|dollars; cents|] -> 100 * Convert.ToInt32 dollars + Convert.ToInt32 cents
        | _ -> 0

    type Employee = {
        mutable ID : int
        mutable FirstName : string
        mutable LastName : string
        mutable JobTitle : Job
        mutable Payment : Pay
        mutable Schedule : Hour[] // 24x
    }

    let serialize<'a> (x :'a) =
        let binFormatter = BinaryFormatter ()
        use stream = MemoryStream ()
        binFormatter.Serialize (stream, x)
        stream.ToArray ()

    let deserialize<'a> (arr : byte[]) =
        let binFormatter = BinaryFormatter ()
        use stream = MemoryStream arr
        binFormatter.Deserialize stream :?> 'a

    let createOrOpen dbFile =
        let mutable init = false
        if not (File.Exists dbFile) then
            SQLiteConnection.CreateFile dbFile
            init <- true
        let conn_string = sprintf "DataSource=%s;Version=3;" dbFile
        let conn = SQLiteConnection (conn_string)
        conn.Open ()
        if init then
            let create = "CREATE TABLE employees (first TEXT NOT NULL,
                                                  last TEXT NOT NULL,
                                                  job TEXT NOT NULL,
                                                  payType TEXT NOT NULL,
                                                  pay INTEGER NOT NULL,
                                                  schedule BLOB NOT NULL)"
            let cmd = SQLiteCommand (create, conn)
            cmd.ExecuteNonQuery () |> ignore
        conn

    let addEmployee conn firstName lastName jobTitle payment schedule =
        let (payType, pay) = match payment with
                             | Salary x -> ("Salary", x)
                             | Wage x -> ("Wage", x)

        let blob = serialize<Hour[]> schedule
        let fParam = SQLiteParameter ("@f", DbType.String, Value=firstName)
        let lParam = SQLiteParameter ("@l", DbType.String, Value=lastName)
        let jParam = SQLiteParameter ("@j", DbType.String, Value=string_of_job jobTitle)
        let tParam = SQLiteParameter ("@t", DbType.String, Value=payType)
        let pParam = SQLiteParameter ("@p", DbType.Int32,  Value=pay)
        let sParam = SQLiteParameter ("@s", DbType.Binary, Value=blob)

        let query = "INSERT INTO employees VALUES (@f, @l, @j, @t, @p, @s)"
        let cmd = SQLiteCommand (query, conn)
        List.map cmd.Parameters.Add [fParam; lParam; jParam; tParam; pParam; sParam] |> ignore
        cmd.ExecuteNonQuery () |> ignore

        let query = "SELECT last_insert_rowid()"
        let cmd = SQLiteCommand (query, conn)
        let id = cmd.ExecuteScalar ()

        { ID = Convert.ToInt32 id
          FirstName = firstName
          LastName = lastName
          JobTitle = jobTitle
          Payment = payment
          Schedule = schedule }

    let updateEmployee conn firstName lastName jobTitle payment =
        let (payType, pay) = match payment with
                             | Salary x -> ("Salary", x)
                             | Wage x -> ("Wage", x)

        let fParam = SQLiteParameter ("@f", DbType.String, Value=firstName)
        let lParam = SQLiteParameter ("@l", DbType.String, Value=lastName)
        let jParam = SQLiteParameter ("@j", DbType.String, Value=string_of_job jobTitle)
        let tParam = SQLiteParameter ("@t", DbType.String, Value=payType)
        let pParam = SQLiteParameter ("@p", DbType.Int32,  Value=pay)

        let query = "UPDATE employees SET first=@f, last=@l, job=@j, payType=@t, pay=@p"
        let cmd = SQLiteCommand (query, conn)
        List.map cmd.Parameters.Add [fParam; lParam; jParam; tParam; pParam] |> ignore
        cmd.ExecuteNonQuery () |> ignore

    let getEmployees conn =
        let query = "SELECT rowid, first, last, job, payType, pay, schedule FROM employees"
        let cmd = SQLiteCommand (query, conn)
        let employees = cmd.ExecuteReader ()
    
        let mutable result = ResizeArray<Employee> ()
        while employees.Read () do
            let isSalary = (employees.["payType"] :?> string) = "Salary"
            let pay = Convert.ToInt32 employees.["pay"]
            result.Add { ID = Convert.ToInt32 employees.["rowid"]
                       ; FirstName = employees.["first"] :?> string
                       ; LastName = employees.["last"] :?> string
                       ; JobTitle = employees.["job"] :?> string |> job_of_string
                       ; Payment = if isSalary then Salary pay else Wage pay
                       ; Schedule = deserialize<Hour[]> (employees.["schedule"] :?> byte[])
                       }
        result

    let deleteEmployee conn id =
        let iParam = SQLiteParameter ("@i", DbType.Int32, Value=id)

        let query = "DELETE FROM employees WHERE rowid = @i"
        let cmd = SQLiteCommand (query, conn)
        cmd.Parameters.Add iParam |> ignore
        cmd.ExecuteNonQuery () |> ignore


    let saveSchedule conn id schedule =
        let blob = serialize<Hour[]> schedule
        let sParam = SQLiteParameter ("@s", DbType.Binary, Value=blob)
        let iParam = SQLiteParameter ("@i", DbType.Int32, Value=id)

        let query = "UPDATE employees SET schedule=@s WHERE rowid=@i"
        let cmd = SQLiteCommand (query, conn)
        List.map cmd.Parameters.Add [sParam; iParam] |> ignore
        cmd.ExecuteNonQuery () |> ignore