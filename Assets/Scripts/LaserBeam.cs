using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeam
{
    public GameObject gameObject {get; set;} // GameObject class is sealed, unfortunately...

    public bool Alive
    {
        get { return _alive; }
        set { _alive = Alive; }
    }

    private const float _timeToLiveSeconds = 1.0f; // After this, a laser ball will be destroyed.
    private const float _speed = 3.0f;

    private readonly Transform _launcherTransform;
    private bool _alive;

    private float _currentLiveTimeSeconds = 0.0f;

    private Vector3 _normalizedDelta;


    public LaserBeam(GameObject gameObject, Transform launcherTransform)
    {
        float angleDegreesZ = 0.0f;

        _alive = true;
        _launcherTransform = launcherTransform;

        this.gameObject = gameObject;
        gameObject.transform.position = _launcherTransform.position;

        angleDegreesZ = _launcherTransform.rotation.eulerAngles.z;
        CountDeltaAxis(angleDegreesZ);
    }

    public void Update()
    {
        float deltaMultiplier = Time.deltaTime * _speed;
        var delta = new Vector3(_normalizedDelta.x * deltaMultiplier,
                                _normalizedDelta.y * deltaMultiplier, 0);

        if ((_currentLiveTimeSeconds += Time.deltaTime) > _timeToLiveSeconds)
        {
            _alive = false;
        }
        gameObject.transform.position += delta; // Move constantly.
    }

    void CountDeltaAxis(float angleDegreesZ)
    {
        float tangent;

        if ((tangent = Mathf.Tan(angleDegreesZ * Mathf.Deg2Rad)) == 0.0f)
        {
            tangent = 100.0f; // Or any relatively big random number.
        }

        // Unity's angles seems to be differ as the <270; 360) deegrees range
        // is the first quarter of a cartesian plane, so angle is shifted -90*.
        //
        // 2-nd quarter.
        if ((0.0f < angleDegreesZ) && (angleDegreesZ < 90.0f))
        {
            _normalizedDelta.x = -tangent;
            _normalizedDelta.y = (1.0f / tangent);
        }
        // 3-nd quarter.
        else if ((90.0f < angleDegreesZ) && (angleDegreesZ < 180.0f))
        {
            _normalizedDelta.x = tangent;
            _normalizedDelta.y = (1.0f / tangent);
        }
        // 4-th quarter.
        else if ((180.0f < angleDegreesZ) && (angleDegreesZ < 270.0f))
        {
            _normalizedDelta.x = tangent;
            _normalizedDelta.y = -(1.0f / tangent);            
        }
        // 1-st quarter.
        else if ((270.0f < angleDegreesZ) && (angleDegreesZ < 360.0f))
        {
            _normalizedDelta.x = -tangent;
            _normalizedDelta.y = -(1.0f / tangent);            
        }
        else if (angleDegreesZ == 0.0f)
        {
            _normalizedDelta.y = 1.0f;
        }
        else if (angleDegreesZ == 90.0f)
        {
            _normalizedDelta.x = -1.0f;            
        }
        else if (angleDegreesZ == 180.0f)
        {
            _normalizedDelta.y = -1.0f;
        }
        else if (angleDegreesZ == 270.0f)
        {
            _normalizedDelta.x = 1.0f;
        }
        _normalizedDelta = Vector3.Normalize(_normalizedDelta);
    }
}
