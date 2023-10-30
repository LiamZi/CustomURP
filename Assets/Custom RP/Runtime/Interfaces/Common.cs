namespace CustomURP
{
    public class Common
    {
        public enum Pass
        {
            Default = 0,
            Before,
            Normal,
            After,
            Async,
            Max
        };

       public enum RenderType
       {
           Default = 0,
           Depth,
           Normal,
           Light,
           Shadow,
           Post, 
           Compute,
           Max
       };
    }
}