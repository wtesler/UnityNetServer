﻿# UnityNetServer 🚀

A lightweight and extensible HTTP server built with Unity, designed to handle local or networked web requests within a Unity application. Allows for the creation of interesting web servers which interact with GameObjects, Meshes, Textures, etc... Perfect for production, prototyping, testing, or creating interactive web-connected experiences inside your Unity projects.

---

## 📦 Features

- ⚙️ Minimal setup within any Unity project
- 🧠 Threaded request handling for main thread vs non-blocking performance
- 🌐 Cross-platform support (Windows, macOS, Linux)
- 🔌 Strongly typed requests and responses.
- 🔥 No 3rd party dependencies
---

## 🛠️ Getting Started

### Prerequisites
- .NET Framework 2.1 or higher

### Installation

1. Clone or download this repository:
   ```bash
   git clone https://github.com/wtesler/UnityNetServer.git
   ```

2. Drop the inner UnityNetServer folder into your Unity project's Assets directory.

3. Create an Empty GameObject in your scene and add the `UnityNetServer` script to it.

### 📘 Usage

1. To create an endpoint in the server, create a Controller script which is derived from the abstract `Controller` class and attach it to the Server GameObject (or a child object inside it).

Implement the abstract methods:

- `GetMethod` : The REST method of the endpoint (GET, POST etc...)
- `GetPath` : The endpoint path (starting with a slash)
- `GetResponseType` : The content type of the response. Common options are "application/json" or "text/plain". See https://www.iana.org/assignments/media-types/media-types.xhtml for the full list.
- `RunOnMainThread` : Whether the endpoint should be handled on the main thread or not. If manipulating the scene or interacting with GameObjects / Materials etc... then it should run on the main thread.
- `Initialize` : Optional override which allows you to initialize any objects in the controller the first time it is used.
- `Handle` : The main function of the controller. This is run when the endpoint is hit. It receives a Request and returns a Response.

Here is an example Controller for a GET request which echos back the query parameter message that is sent to it:

```
using System;
using System.Net;
using System.Threading.Tasks;
using wtesler.UnityNetServer;

public class EchoController : Controller {
  public override Method GetMethod() {
    return Method.GET;
  }

  public override string GetPath() {
    return "/echo";
  }
  
  public override string GetResponseType() {
    return "application/json";
  }

  public override bool RunOnMainThread() {
    return false;
  }

  public override void Initialize() {
  }

  public override async Task<Response> Handle(HttpListenerRequest request) {
    var echoRequest = await RequestHelper.ParseRequest<EchoRequest>(request, GetMethod());

    return new Response {
      data = new ResponseData {
        echo = echoRequest.message
      } 
    };
  }

  private class EchoRequest {
    public string message { get; set; }
  }
  
  [Serializable]
  private class ResponseData {
    public string echo;
  }
}

```

Can be tried out like `http://localhost:8080/echo?message=Hello`

#### Requests

The controllers receive a `HttpListenerRequest`. It can be converted into a strongly typed request object using the provided `RequestHelper` functions. For example:

`var echoRequest = await RequestHelper.ParseRequest<EchoRequest>(request, GetMethod());`

Where `EchoRequest` looks like:

```
private class EchoRequest {
    public string message { get; set; }
}
```

If the method is `GET`, it will convert the query parameters to the request object.

If the method is `POST`, it will convert the body into the request object.

#### Response

The Response object is a simple object that has a serializable data field and status code.

In the case of "application/json" response type, the server will handle the serialization of the data object to a JSON buffer.

### 🚀 Deployment

The server can be included in a Unity Dedicated Server build and deployed in a dockerized container.

I have had success deploying the server to production using a Linux Dedicated Server build and Google Cloud Run server. I have included the Dockerfile I used for this in the Extras folder. I build the application to a server folder and deploy from there.

I have also included a useful Google Cloud utility file in extras which I use to deploy to production. It should be placed next to the Dockerfile and the server folder.

### 📄 License
MIT License. See LICENSE for details.

### ⚠️ Limitations

- Only `GET` and `POST` requests handled but it should be very straightforward to add support to others.
- Only `JSON` and `Text` responses handled but it should be very straightforward to add support to others.
- `IL2CPP` might be required for production servers but it is worth testing that out.
