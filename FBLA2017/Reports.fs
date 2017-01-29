namespace FEC

module Reports =
    open FsXaml
    open System
    open System.Linq
    open System.Windows
    open FSharp.Charting
    open System.Windows.Controls
    open System.Drawing.Printing
    open Microsoft.FSharp.Reflection
    open System.Windows.Media.Imaging

    open FEC.Data

    let showChart (chart : ChartTypes.GenericChart) =
        let cancel (form : Forms.Form) _ _ =
            form.Close ()
        let print _ _ =
            chart.SaveChartAs ("report.png", ChartTypes.ChartImageFormat.Png)
            let img = Drawing.Image.FromFile "report.png"
            let doc = PrintDocument ()
            doc.PrintPage.Add (fun e ->
                e.Graphics.DrawImage(img, Drawing.Point(0, 0)))
            doc.Print()
        let form = chart.ShowChart ()
        form.Menu <- Forms.MainMenu [|
            Forms.MenuItem ("Cancel", EventHandler (cancel form));
            Forms.MenuItem ("Print", EventHandler print)
        |]

    let peakDays conn =
        let allWeeks = Data.getAllAttendance conn
        let addWeeks acc week = { Sunday = acc.Sunday + week.Sunday
                                ; Monday = acc.Monday + week.Monday
                                ; Tuesday = acc.Tuesday + week.Tuesday
                                ; Wednesday = acc.Wednesday + week.Wednesday
                                ; Thursday = acc.Thursday + week.Thursday
                                ; Friday = acc.Friday + week.Friday
                                ; Saturday = acc.Saturday + week.Saturday
                                }
        let sum = allWeeks.ToArray () |> Seq.map (Seq.reduce addWeeks) |> Seq.reduce addWeeks
        let fields = FSharpType.GetRecordFields (sum.GetType ())
        
        Seq.map (fun (field : Reflection.PropertyInfo) -> field.Name, field.GetValue sum :?> int) fields
        |> Chart.Line
        |> Chart.WithXAxis (Title = "Day of Week", LabelStyle = ChartTypes.LabelStyle (Angle = -45, Interval = 1.0))
        |> Chart.WithYAxis (Title = "Number of Customers")
        |> showChart

    let peakHours conn =
        let allWeeks = Data.getAllAttendance conn
        let addDays week = week.Sunday + week.Monday + week.Tuesday + week.Wednesday + week.Thursday + week.Friday + week.Saturday
        let sums = allWeeks.ToArray () |> Array.map (Array.map addDays)
        let hourSums = Array.fold (fun a b -> [| for i in 0..23 -> Seq.item i a + Seq.item i b |]) (Array.init 24 (fun _ -> 0)) sums

        let labels =
            let hour_of_int i =
                let hour = if i % 12 = 0 then 12 else i % 12
                let period = if i < 12 then "AM" else "PM"
                sprintf "%i:00 %s" hour period
            List.map hour_of_int [0..23]

        Seq.zip labels hourSums
        |> Chart.Line
        |> Chart.WithXAxis (Title = "Time of Day", LabelStyle = ChartTypes.LabelStyle (Angle = -45, Interval = 1.0))
        |> Chart.WithYAxis (Title = "Number of Customers")
        |> showChart