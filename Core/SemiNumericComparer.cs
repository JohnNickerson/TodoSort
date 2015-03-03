using System;
// From: http://stackoverflow.com/questions/6396378/c-sharp-linq-orderby-numbers-that-are-string-and-you-cannot-convert-them-to-int
// By: Jeff Paulsen (http://stackoverflow.com/users/16199/jeff-paulsen)
using System.Collections.Generic;
public class SemiNumericComparer: IComparer<string>
{
    public int Compare(string s1, string s2)
    {
        if (IsNumeric(s1) && IsNumeric(s2))
        {
            if (Convert.ToInt32(s1) > Convert.ToInt32(s2)) return 1;
            if (Convert.ToInt32(s1) < Convert.ToInt32(s2)) return -1;
            if (Convert.ToInt32(s1) == Convert.ToInt32(s2)) return 0;
        }

        if (IsNumeric(s1) && !IsNumeric(s2))
            return -1;

        if (!IsNumeric(s1) && IsNumeric(s2))
            return 1;

        return string.Compare(s1, s2, true);
    }

    public static bool IsNumeric(object value)
    {
        try {
            int i = Convert.ToInt32(value.ToString());
            return true; 
        }
        catch (FormatException) {
            return false;
        }
    }
}