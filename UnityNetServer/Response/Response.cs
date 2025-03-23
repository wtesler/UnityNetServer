using System;

namespace wtesler.UnityNetServer {
  [Serializable]
  public class Response {
    public object data;
    public int statusCode;
  }
}
