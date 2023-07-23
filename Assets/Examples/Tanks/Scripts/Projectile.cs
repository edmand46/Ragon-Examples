using Ragon.Client;
using Ragon.Client.Unity;
using UnityEngine;

namespace Ragon.Examples.Tanks
{
    public class Projectile : RagonBehaviour
    {
        public float destroyAfter = 2;
        public Rigidbody rigidBody;
        public float force = 1000;
        
        void Start()
        {
            rigidBody.AddForce(transform.forward * force);
        }
    }
}
