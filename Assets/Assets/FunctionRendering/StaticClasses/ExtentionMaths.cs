public static class Maths
{
    //Implement a new function Maths.Min to allow multiple params
    public static double Min(double num1,double num2, params double[] extraNum)
    {
        //Treating num1 as smallest number container variable
        if(num2<num1)
        {
            num1 = num2;
        }
        foreach (double number in extraNum)
        {
            if (number < num1)
            {
                num1 = number;
            }
        }
        return num1;
    }
    //Implement a new function Maths.Max to allow multiple params
    public static double Max(double num1, double num2, params double[] extraNum)
    {
        //Treating num1 as largest number container variable
        if (num2 > num1)
        {
            num1 = num2;
        }
        foreach (double number in extraNum)
        {
            if (number > num1)
            {
                num1 = number;
            }
        }
        return num1;
    }
}