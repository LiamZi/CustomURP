using System;
using System.Diagnostics;


namespace Core
{
    public class AssertException : Exception
    {
        public AssertException()
        {
            
        }

        public AssertException(string msg)
            : base(msg)
        {
            
        }
    };

    public static class Assert
    {
        [Conditional("DEBUG")]
        public static void Fail()
        {
            throw new AssertException();
        }

        [Conditional("DEBUG")]
        public static void Fail(string error)
        {
            throw new AssertException(error);
        }

        [Conditional("DEBUG")]
        public static void Fail(string format, params object[] args)
        {
            throw new AssertException(string.Format(format, args));
        }

        [Conditional("DEBUG")]
        public static void Check(bool condition)
        {
            if (condition == false)
            {
                throw new AssertException();
            }
        }

        [Conditional("DEBUG")]
        public static void Check(bool condition, string error)
        {
            if (condition == false)
            {
                throw new AssertException(error);
            }
        }

        [Conditional("DEBUG")]
        public static void Check(bool condition, string format, params object[] args)
        {
            if (condition == false)
            {
                throw new AssertException(string.Format(format, args));
            }
        }

        public static void Always(bool condition)
        {
            if (condition == false)
            {
                throw new AssertException();
            }
        }

        public static void Always(bool condition, string error)
        {
            if (condition == false)
            {
                throw new AssertException(error);
            }
        }

        public static void Always(bool condition, string format, params object[] args)
        {
            if (condition == false)
            {
                throw new AssertException(string.Format(format, args));
            }
        }
    };

}