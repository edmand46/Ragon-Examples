using Example.Game;
using Ragon.Client;
using Ragon.Client.Unity;
using Ragon.Examples.Tanks;
using Tanks.Scripts.Events;
using UnityEngine;
using UnityEngine.AI;

namespace Mirror.Examples.Tanks
{
  public class Tank : RagonBehaviour
  {
    public NavMeshAgent agent;
    public Animator animator;
    public TextMesh healthBar;
    public Transform turret;

    public float rotationSpeed = 100;

    public KeyCode shootKey = KeyCode.Space;
    public GameObject projectilePrefab;
    public Transform projectileMount;

    [SerializeField] public RagonInt health = new RagonInt(4);
    [SerializeField] public RagonVector3 _position = new RagonVector3(Vector3.zero);
    [SerializeField] public RagonFloat _rotationBody = new RagonFloat(4);
    [SerializeField] public RagonFloat _rotationHead = new RagonFloat(4);

    public override void OnAttachedEntity()
    {
      if (Entity.HasAuthority)
      {
        var payload = Entity.GetAttachPayload<TankPayload>();
        transform.position = payload.Position;
        
        FindObjectOfType<FollowCamera>().SetFollow(transform);
      }
    
      
      Entity.OnEvent<FireEvent>(OnFire);
      
      var subscription = Entity.OnEvent<FireEvent>(OnFire);
      // subscription.Dispose();
      
      health.Changed += () => healthBar.text = new string('-', health.Value);
    }

    private void OnFire(RagonPlayer invoker, FireEvent evnt)
    { 
      Debug.Log("Fire");
      Instantiate(projectilePrefab, projectileMount.position, projectileMount.rotation);
      animator.SetTrigger("Shoot");
    }

    public override void OnUpdateEntity()
    {
      float horizontal = Input.GetAxis("Horizontal");
      float vertical = Input.GetAxis("Vertical");

      transform.Rotate(0, horizontal * rotationSpeed * Time.deltaTime, 0);

      Vector3 forward = transform.TransformDirection(Vector3.forward);

      agent.velocity = forward * Mathf.Max(vertical, 0) * agent.speed;
      animator.SetBool("Moving", agent.velocity != Vector3.zero);

      if (Input.GetKeyDown(shootKey))
        Entity.ReplicateEvent(new FireEvent());

      RotateTurret();

      _position.Value = transform.position;
      _rotationBody.Value = transform.eulerAngles.y;
      _rotationHead.Value = turret.eulerAngles.y;
    }

    void OnTriggerEnter(Collider other)
    {
      if (other.GetComponent<Projectile>() != null)
      {
        Destroy(other.gameObject);
        
        health.Value -= 1;
        
        if (health.Value == 0)
         RagonNetwork.Destroy(gameObject);
      }
    }

    public override void OnUpdateProxy()
    {
      transform.position = Vector3.Lerp(transform.position, _position.Value, Time.deltaTime * 10);;
      transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, _rotationBody.Value, 0), Time.deltaTime * 15);
      turret.rotation = Quaternion.Lerp(turret.rotation, Quaternion.Euler(0, _rotationHead.Value, 0), Time.deltaTime * 15);
    }
    
    
    void RotateTurret()
    {
      Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
      RaycastHit hit;
      if (Physics.Raycast(ray, out hit, 100))
      {
        Debug.DrawLine(ray.origin, hit.point);
        Vector3 lookRotation = new Vector3(hit.point.x, turret.transform.position.y, hit.point.z);
        turret.transform.LookAt(lookRotation);
      }
    }
  }
}