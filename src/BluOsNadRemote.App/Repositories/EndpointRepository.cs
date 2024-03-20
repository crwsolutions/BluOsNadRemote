using BluOsNadRemote.App.Models;

namespace BluOsNadRemote.App.Services;

public sealed partial class EndpointRepository
{
    [Dependency]
    private readonly IPreferences _preferences;

    private const string ENDPOINT_NAME = "endname";
    private const string ENDPOINT_URL = "endurl";
    private const string ENDPOINT_SELECTED = "selected";
    private const string ENDPOINTS_LENGTH = "length";

    private EndPoint _selectedEndpoint;

    public EndPoint SelectedEndpoint
    {
        get
        {
            if (_selectedEndpoint is not null)
            {
                return _selectedEndpoint;
            }

            var selected = _preferences.Get(ENDPOINT_SELECTED, 0);
            return _selectedEndpoint ??= GetEndPoint(selected);
        }
        set
        {
            var endPoints = GetEndPoints();
            for (var i = 0; i < endPoints.Length; i++)
            {
                if (endPoints[i].Equals(value))
                {
                    UpdateSelecteEndpoint(i);
                    break;
                }
            }
            _selectedEndpoint = value;
        }
    }

    public EndPoint[] GetEndPoints()
    {
        var selected = _preferences.Get(ENDPOINT_SELECTED, 0);
        var length = _preferences.Get(ENDPOINTS_LENGTH, 0);
        var endpoints = new EndPoint[length];
        for (var i = 0; i < length; i++)
        {
            endpoints[i] = GetEndPoint(i);
        }

        return endpoints;
    }

    public void MergeEndpoints(EndPoint[] newEndpoints)
    {
        if (newEndpoints == null || newEndpoints.Length == 0)
        {
            return;
        }

        var oldEndpoints = GetEndPoints();

        if (oldEndpoints is null || oldEndpoints.Length == 0)
        {
            UpdateSelecteEndpoint(0);
            SetEndPoints(newEndpoints);
            return;
        }

        var mergeList = new List<EndPoint>(oldEndpoints);

        foreach (var newEndpoint in newEndpoints)
        {
            var old = mergeList.FirstOrDefault(old => old.Uri == newEndpoint.Uri);
            if (old is not null)
            {
                old.LastKnowName = newEndpoint.LastKnowName;
            }
            else
            {
                mergeList.Add(newEndpoint);
            }
        }

        SetEndPoints([.. mergeList]);
    }

    private void UpdateSelecteEndpoint(int i)
    {
        _preferences.Set(ENDPOINT_SELECTED, i);
        _selectedEndpoint = null;
    }

    private void SetEndPoints(EndPoint[] endPoints)
    {
        ClearEndpoints();
        for (var i = 0; i < endPoints.Length; i++)
        {
            var endpoint = endPoints[i];
            _preferences.Set(ENDPOINT_URL + i, endpoint.Uri.ToString());
            _preferences.Set(ENDPOINT_NAME + i, endpoint.LastKnowName);
        }
        _preferences.Set(ENDPOINTS_LENGTH, endPoints.Length);
    }

    private EndPoint GetEndPoint(int index)
    {
        var uri = _preferences.Get<string>(ENDPOINT_URL + index, null);

        if (uri is null)
        {
            return null;
        }

        var name = _preferences.Get<string>(ENDPOINT_NAME + index, null);
        return new EndPoint(uri, name);
    }

    internal void ClearEndpoints()
    {
        var length = _preferences.Get(ENDPOINTS_LENGTH, 0);
        for (var i = 0; i < length; i++)
        {
            _preferences.Remove(ENDPOINT_URL + i);
            _preferences.Remove(ENDPOINT_NAME + i);
            Debug.WriteLine($"Removed '{ENDPOINT_URL + i}' and '{ENDPOINT_NAME + i}'");
        }
        _preferences.Remove(ENDPOINTS_LENGTH);
        _preferences.Remove(ENDPOINT_SELECTED);
    }
}
