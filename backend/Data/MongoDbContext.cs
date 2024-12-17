using MongoDB.Driver;
using MongoDB.Bson;

namespace MyApp
{
    class Program
    {
        static void Main(string[] args)
        {
            const string connectionUri = "mongodb+srv://priyanshusamal1234:%23Ppu8urlcc@sentimatrix.euu2q.mongodb.net/?retryWrites=true&w=majority&appName=Sentimatrix";
            var settings = MongoClientSettings.FromConnectionString(connectionUri);

            // Set the ServerApi field of the settings object to set the version of the Stable API on the client
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);

            // Create a new client and connect to the server
            var client = new MongoClient(settings);

            // Send a ping to confirm a successful connection
            try
            {
                var result = client.GetDatabase("sentimatrix").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                Console.WriteLine("Pinged your deployment. You successfully connected to MongoDB!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}