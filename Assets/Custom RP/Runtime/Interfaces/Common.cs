namespace CustomURP
{
    public class Common
    {
        public enum Pass
        {
            Normal = 0,
            Before,
            After,
            Async,
        };

       public enum RenderType
       {
           Normal = 0,
           Light,
           Shadow,
           Post
       };
    }
}