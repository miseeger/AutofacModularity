using Autofac;

namespace AutofacModularity
{
    
    public sealed class DiRepository
    {

        private static DiRepository _instance = null;
        private static readonly object Padlock = new object();

        public IContainer Container { get; set; }


        DiRepository()
        {
        }


        public static DiRepository Instance
        {
            get
            {
                lock (Padlock)
                {
                    return _instance ?? (_instance = new DiRepository());
                }
            }
        }

    }  

}