using System;
using System.Collections.Generic;

public class QueryParams
{
    public String path { get; set; }
}

public class Request
{
    public String httpMethod { get; set; }
    public Dictionary<string, string> headers { get; set; }
    public String url { get; set; }
    public QueryParams queryStringParameters { get; set; }
    public String body { get; set; }
    public bool isBase64Encoded { get; set; }
}
