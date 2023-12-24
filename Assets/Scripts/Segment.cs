using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;

public class Segment : MonoBehaviour
{
    [SerializeField]
    protected GameObject segmentPref;
    public static float speed = 1f;
    public static int snakeLen;
    protected Rigidbody2D _rb;
    [SerializeField]
    float currentT = 1;
    [SerializeField] bool isTail = false;
    [SerializeField] bool isHead = false;

    public Segment ForwardSegment
    {
        get { return forwardSegment; }
    }
    [SerializeField]
    protected Segment forwardSegment;

    public Segment BackwardSegment
    {
        get { return backwardSegment; }
    }
    [SerializeField]
    protected Segment backwardSegment;

    protected void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        //currentT -= speed * Time.fixedDeltaTime;
    }

    void FixedUpdate()
    {
        Vector2 moveVector = transform.up;

        if (isHead)
        {
            Vector2 nearFromDirection;
            Vector2 nearFrom = Railway.GetLastPointOnRailway(out nearFromDirection);
            Vector2 newDir;
            if (Input.GetAxis("Vertical") != 0)
            {
                newDir = Input.GetAxis("Vertical") > 0 ? Vector2.up : Vector2.down;
                if (Mathf.Abs(moveVector.y) <= 0.001f)
                {
                    Railway.AddRail(nearFrom, nearFrom + nearFromDirection/2+newDir/2);
                }
            }
            else if (Input.GetAxis("Horizontal") != 0)
            {
                newDir = Input.GetAxis("Horizontal") > 0 ? Vector2.right : Vector2.left;
                if (Mathf.Abs(moveVector.x) <= 0.001f)
                {
                    Railway.AddRail(nearFrom, nearFrom + nearFromDirection/2 + newDir / 2);
                }
            }
            else if(currentT >= Mathf.Ceil(currentT)-0.21f)
            {
                // no chose for move
                Railway.AddRail(nearFrom, nearFrom + nearFromDirection);
            }
        }

        Railway.GetPositionOnRailway(currentT, out moveVector);
        this.transform.eulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, moveVector));

        _rb.velocity = moveVector.normalized * speed * Time.fixedDeltaTime;
        currentT += _rb.velocity.magnitude * Time.fixedDeltaTime;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isHead)
            {
                AddNextSegment();
            }
        }
        if(isHead)
        {
            for (float i = 0; i < currentT; i += 0.02f)
            {
                Debug.DrawLine(Railway.GetPositionOnRailway(i), Railway.GetPositionOnRailway(i + 0.02f), Color.blue);
            }
        }
    }

    protected GameObject AddNextSegment()
    {
        Segment newSegment;
        newSegment = Instantiate(segmentPref, backwardSegment.transform.position, backwardSegment.transform.rotation).GetComponent<Segment>();
        newSegment.forwardSegment = this;
        newSegment.backwardSegment = backwardSegment;
        backwardSegment.forwardSegment = newSegment;
        newSegment.transform.rotation = backwardSegment.transform.rotation;
        backwardSegment.MoveSegmentToBackward();


        backwardSegment = newSegment;
        snakeLen++;


        return newSegment.gameObject;
    }
    public GameObject AddNextSegments(int n)
    {
        GameObject newSegment = null;
        for (int i = 0; i < n; i++)
        {
            newSegment = AddNextSegment();
        }

        return newSegment;
    }

    void MoveSegmentToForward()
    {
        if (!isHead)
        {
            transform.position = forwardSegment.transform.position;
            if (!isTail)
                backwardSegment.MoveSegmentToForward();
        }
    }

    void MoveSegmentToBackward()
    {
        if (isTail)
        {
            transform.position = transform.position - transform.up;
        }
        else
        {
            transform.position = backwardSegment.transform.position;
            transform.rotation = BackwardSegment.transform.rotation;
            backwardSegment.MoveSegmentToBackward();
        }

    }

    void OnDestroy()
    {
        //backwardSegment.forwardSegment = forwardSegment;
        //backwardSegment.MoveSegmentToForward();
        snakeLen--;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Food"))
        {
            AddNextSegment();
            Destroy(collision.gameObject);
        }
    }

    public static class Railway
    {
        static List<Rail> rails = new List<Rail>();
        public static void AddRail(Vector2 from, Vector2 to)
        {
            rails.Add(new Rail(from, to));
        }
        public static Vector2 GetPositionOnRailway(float t)
        {
            int _t = (int)t;
            return rails[_t].GetRailPos(t - _t);
        }
        public static Vector2 GetPositionOnRailway(float t, out Vector2 direction)
        {
            int _t = (int)t;
            return rails[_t].GetRailPos(t - _t, out direction);
        }
        public static Vector2 GetLastPointOnRailway()
        {
            return rails.Last().GetRailPos(1);
        }
        public static Vector2 GetLastPointOnRailway(out Vector2 direction)
        {
            return rails.Last().GetRailPos(1, out direction);
        }

        class Rail
        {
            public Rail(Vector2 from, Vector2 to)
            {
                this.from = from; this.to = to;
            }

            public bool IsCircle
            {
                get { return !(from.x == to.x) || (from.y == to.y); }
            }

            Vector2 from, to;
            public Vector2 GetRailPos(float t)
            {
                Vector2 res;
                if (!IsCircle)
                {
                    // is line
                    res = Vector2.Lerp(from, to, t);
                }
                else
                {
                    // is circle
                    t /= Mathf.PI / 2;
                    int x, y;
                    float signX, signY;
                    if (from.x == Mathf.Floor(from.x))
                    {
                        x = ((int)to.x);
                        y = ((int)from.y);
                        signX = Mathf.Sign(from.x - x);
                        signY = Mathf.Sign(to.y - y);
                    }
                    else
                    {
                        x = ((int)from.x);
                        y = ((int)to.y);
                        signX = Mathf.Sign(to.x - x);
                        signY = Mathf.Sign(from.y - y);
                    }

                    var offset = new Vector2(x, y);
                    res = new Vector2(signX * 0.5f * Mathf.Cos(t), signY * 0.5f * Mathf.Sin(t)) + offset;
                }

                return res;
            }
            public Vector2 GetRailPos(float t, out Vector2 direction)
            {
                Vector2 res;
                if (!IsCircle)
                {
                    // is line
                    res = Vector2.Lerp(from, to, t);
                    direction = to - from;
                }
                else
                {
                    // is circle
                    t /= Mathf.PI / 2;
                    int x, y;
                    float signX, signY;
                    if (from.x == Mathf.Floor(from.x))
                    {
                        x = ((int)to.x);
                        y = ((int)from.y);
                        signX = Mathf.Sign(from.x - x);
                        signY = Mathf.Sign(to.y - y);
                    }
                    else
                    {
                        x = ((int)from.x);
                        y = ((int)to.y);
                        signX = Mathf.Sign(to.x - x);
                        signY = Mathf.Sign(from.y - y);
                    }

                    var offset = new Vector2(x, y);
                    res = new Vector2(signX * 0.5f * Mathf.Cos(t), signY * 0.5f * Mathf.Sin(t)) + offset;
                    direction = new Vector2(-signX * Mathf.Sin(t), signY * Mathf.Cos(t));
                }

                return res;
            }
        }
    }
}
