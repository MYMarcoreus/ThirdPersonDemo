namespace Utils
{
    public abstract class ManagerSingleton<T> 
        where T: class, new()
    {
        public static T instance;
        
        /// <summary>
        /// 静态方法显式构造单例
        /// </summary>
        public static void OnInstanceInit()
        {
            instance = new T();
        } 
        
        /// <summary>
        /// 静态方法显式析构单例
        /// </summary>
        public static void OnInstanceDispose()
        {
            instance = null;
        } 
        
    }
}