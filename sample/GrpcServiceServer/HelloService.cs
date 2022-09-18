namespace GrpcServiceServer
{
    public class HelloService
    {
        public string SayHello(string name)
        {
            return "HelloService SayHello: Hello " + name;
        }
    }
}
