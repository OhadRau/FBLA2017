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

type MoneyConverter () =
    interface System.Windows.Data.IValueConverter with
        member self.Convert (value, _, _, _) = pay_string_of_int (value :?> int) :> obj
        member self.ConvertBack (_, _, _, _) = null