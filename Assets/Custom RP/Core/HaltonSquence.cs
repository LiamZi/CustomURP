namespace Core
{
    public class HaltonSquence
    {
        public int _radix = 3;
        int _storedIndex = 0;

        public float Get()
        {
            float result = 0f;
            float fraction = 1f / (float)_radix;
            int index = _storedIndex;
            while (index > 0)
            {
                result += (float)(index % _radix) * fraction;
                index /= _radix;
                fraction /= (float)_radix;
            }
            _storedIndex++;
            return result;
        }
    };
}
