namespace FEC.Converters

open FEC.Data

type JobConverter () =
    interface System.Windows.Data.IValueConverter with
        member self.Convert (value, _, _, _) = string_of_job (value :?> Job) :> obj
        member self.ConvertBack (_, _, _, _) = null

type PayConverter () =
    interface System.Windows.Data.IValueConverter with
        member self.Convert (value, _, _, _) = string_of_pay (value :?> Pay) :> obj
        member self.ConvertBack (_, _, _, _) = null
