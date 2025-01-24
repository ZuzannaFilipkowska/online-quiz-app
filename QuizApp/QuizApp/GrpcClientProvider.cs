using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using QuizApp;
using System;
using System.Net.Http;

public class GrpcClientProvider
{
    private static readonly Lazy<GrpcClientProvider> _instance =
        new Lazy<GrpcClientProvider>(() => new GrpcClientProvider());

    private readonly QuizService.QuizServiceClient _grpcClient;
    private readonly GrpcChannel _channel;

    private GrpcClientProvider()
    {
        var serverAddress = "http://10.0.2.2:5195";
        _channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler())
        });
        _grpcClient = new QuizService.QuizServiceClient(_channel);
    }

    public static GrpcClientProvider Instance => _instance.Value;

    public QuizService.QuizServiceClient GetClient() => _grpcClient;
}