using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;


public class Handler
{
    string PREFIX;
    string BUCKET;

    public static void Main(string[] args)
    {

    }



    public async Task<Response> FunctionHandler(Request request)
    {
        try
        {
            String AWS_ACCESS_KEY = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");
            String AWS_SECRET_KEY = Environment.GetEnvironmentVariable("AWS_SECRET_KEY");
            BUCKET = Environment.GetEnvironmentVariable("BUCKET");
            PREFIX = Environment.GetEnvironmentVariable("PREFIX");

            AmazonS3Config configsS3 = new AmazonS3Config { 
                ServiceURL = "https://s3.yandexcloud.net"
            };

            AmazonS3Client s3client = new AmazonS3Client(
                    AWS_ACCESS_KEY,
                    AWS_SECRET_KEY,
                    configsS3
                    );

            string path = request.queryStringParameters.path;

            string pattern = "((\\d+)x(\\d+))\\/(.*)";
            var match = Regex.Match(path, pattern);
            string dimensions = match.Groups[1].ToString();
            int width = Int32.Parse(match.Groups[2].ToString());
            int height = Int32.Parse(match.Groups[3].ToString());
            string originalKey = match.Groups[4].ToString();

            string fileTypePattern = "(.jpg|.png)$";
            var extention = Regex.Matches(originalKey, fileTypePattern);


            Console.WriteLine($"bucket: {BUCKET}, key: {PREFIX}/{ originalKey}");

            GetObjectRequest getRequest = new GetObjectRequest
            {
                BucketName = BUCKET,
                Key = $"{PREFIX}/{ originalKey}"
            };

            byte[] result;
            using (GetObjectResponse response = await s3client.GetObjectAsync(getRequest))
            using (Stream responseStream = response.ResponseStream)

            {
                string contentType = response.Headers["Content-Type"];

                using (Image image = Image.Load(responseStream))
                {

                    // Resize the image in place and return it for chaining.
                    // 'x' signifies the current image processing context.
                    image.Mutate(x => x.Resize(width, height));

                    using (MemoryStream ms = new MemoryStream())
                    {
                        IImageEncoder encoder;
                        switch (contentType)
                        {
                            case "image/jpg":
                                {
                                    encoder = new JpegEncoder();
                                    ((JpegEncoder)encoder).Quality = 50;
                                    break;
                                }
                            case "image/png":
                                {
                                    encoder = new PngEncoder();
                                    ((PngEncoder)encoder).ColorType = PngColorType.RgbWithAlpha;
                                    break;
                                }
                            default:
                                return new Response(400, "Unknown Content-Type", new Dictionary<string, string> { });

                        }


                        image.Save(ms, encoder);
                        result = ms.ToArray();
                        var fileTransferUtility = new TransferUtility(s3client);

                        await fileTransferUtility.UploadAsync(ms, BUCKET, $"{PREFIX}/{path}");

                    }

                }
            }

            Console.WriteLine("Successfully uploaded " + path);
            

            return new Response(200, Convert.ToBase64String(result), isBase64Encoded: true);

        }
        catch (Exception e)
        {
            Console.WriteLine("Err {0} {1}", e.Message, e.StackTrace);
            return new Response(500, "", new Dictionary<string, string> { });
        }
    }

}