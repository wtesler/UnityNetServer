using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace wtesler.UnityNetServer {
  public abstract class Controller : MonoBehaviour {
    private bool isInitialized;

    public abstract Method GetMethod();

    public abstract string GetPath();

    public abstract string GetResponseType();

    public abstract bool RunOnMainThread();

    public virtual void Initialize() {
    }

    public abstract Task<Response> Handle(HttpListenerRequest request);

    public bool IsInitialized() {
      return isInitialized;
    }

    public void InitializeInternal() {
      isInitialized = true;
    }

    public enum Method {
      GET,
      POST,
      PUT,
      DELETE
    }
  }
}
