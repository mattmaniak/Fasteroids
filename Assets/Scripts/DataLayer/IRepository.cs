using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRepository
{
    // Not the most beautiful usage of the interface, with predefined return type.
    SpaceShipConfig LoadData();
}
