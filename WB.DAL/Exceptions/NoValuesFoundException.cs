namespace WB.DAL.Exceptions;

public class NoValuesFoundException : Exception
{
    public NoValuesFoundException () : base ("No values has been found") { }

}