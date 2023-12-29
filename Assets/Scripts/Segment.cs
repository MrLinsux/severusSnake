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
    }

    void FixedUpdate()
    {
        Vector2 moveVector = transform.up;

        if (isHead)
        {
            Vector2 nearFrom = Railway.LastRail.GetRailPos(0, out Vector2 nearFromDirection);
            Vector2 lastPoint = Railway.LastRail.GetRailPos(1, out Vector2 lastPointDirection);
            Vector2 newDir;
            if(currentT >= Railway.MaxT-1)
            {
                Railway.AddRail(lastPoint, lastPoint + lastPointDirection);
                nearFrom = Railway.LastRail.GetRailPos(0, out nearFromDirection);
            }
            if (Input.GetAxis("Vertical") != 0)
            {
                newDir = Input.GetAxis("Vertical") > 0 ? Vector2.up : Vector2.down;
                if (Mathf.Abs(nearFromDirection.y) <= 0.001f)
                {
                    Railway.LastRail = new Railway.Rail(nearFrom, nearFrom + nearFromDirection / 2 + newDir / 2);
                }
            }
            else if (Input.GetAxis("Horizontal") != 0)
            {
                newDir = Input.GetAxis("Horizontal") > 0 ? Vector2.right : Vector2.left;
                if (Mathf.Abs(nearFromDirection.x) <= 0.001f)
                {
                    Railway.LastRail = new Railway.Rail(nearFrom, nearFrom + nearFromDirection / 2 + newDir / 2);
                }
            }
            else if(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized == (Vector2)transform.up)
            {
                // if press forward
                Railway.LastRail = new Railway.Rail(nearFrom, nearFrom+ nearFromDirection);
            }
        }

        currentT += speed * Time.fixedDeltaTime * 1.12f;
        var newPos = Railway.GetPositionOnRailway(currentT, out moveVector);
        this.transform.eulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.up, moveVector));

        _rb.MovePosition(newPos);
        //_rb.velocity = moveVector.normalized * speed * Time.fixedDeltaTime;
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
        newSegment.currentT = currentT;
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
            currentT++;
            transform.position = forwardSegment.transform.position;
            if (!isTail)
                backwardSegment.MoveSegmentToForward();
        }
    }

    void MoveSegmentToBackward()
    {
        currentT--;
        if (isTail)
        {
            //transform.position = transform.position - transform.up;
        }
        else
        {
            //transform.position = backwardSegment.transform.position;
            //transform.rotation = BackwardSegment.transform.rotation;
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
        if(collision.attachedRigidbody.bodyType == RigidbodyType2D.Static)
        {
            // is wall
            Debug.Break();
        }
    }

    public static class Railway
    {
        public static Rail LastRail { 
            get 
            { 
                return rails.Last(); 
            } 
            set 
            {
                rails[rails.Count - 1] = value;
                //Debug.Log($"Rail type of {rails[rails.Count - 1].IsCircle}: From {rails[rails.Count - 1].From} To {rails[rails.Count - 1].To}");
            }
        }
        static List<Rail> rails = new List<Rail>();
        public static float MaxT { get { return rails.Count; } }
        public static void AddRail(Vector2 from, Vector2 to)
        {
            rails.Add(new Rail(from, to));
            //string type = rails.Last().IsCircle ? "Circle" : "Line";
            //Debug.Log($"New rail type of {type}: From {from} To {to}");
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

        public class Rail
        {
            public Rail(Vector2 from, Vector2 to)
            {
                this.from = from; this.to = to;
            }

            public bool IsCircle
            {
                get { return !(MathF.Abs(from.x - to.x) <= 0.001f | MathF.Abs(from.y - to.y)<=0.001f); }
            }

            public Vector2 From { get { return from; } }
            public Vector2 To { get { return to; } }

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
                    t *= Mathf.PI / 4;
                    Vector2 cell, center, sign;
                    if (from.x - Mathf.Floor(from.x) <= 0.01f)
                    {
                        cell = new Vector2(from.x, to.y);
                        center = new Vector2(to.x, from.y);
                    }
                    else
                    {
                        cell = new Vector2(to.x, from.y);
                        center = new Vector2(from.x, to.y);
                    }

                    sign = 2 * (cell - center);

                    res = new Vector2(sign.x * 0.5f,0) + center;
                    if((res-from).magnitude > 0.01f)
                    {
                        t = Mathf.PI / 4 - t;
                    }
                    
                    res = new Vector2(sign.x * 0.5f * Mathf.Cos(2 * t), sign.y * 0.5f * Mathf.Sin(2 * t)) + center;
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
                    t *= Mathf.PI / 4;
                    Vector2 cell, center, sign;
                    if (from.x - Mathf.Floor(from.x) <= 0.01f)
                    {
                        cell = new Vector2(from.x, to.y);
                        center = new Vector2(to.x, from.y);
                    }
                    else
                    {
                        cell = new Vector2(to.x, from.y);
                        center = new Vector2(from.x, to.y);
                    }

                    sign = 2*(cell - center);
                    //Debug.DrawLine(cell, center, Color.red, 5f);

                    res = new Vector2(sign.x * 0.5f, 0) + center;
                    direction = new Vector2(-sign.x * Mathf.Sin(2 * t), sign.y * Mathf.Cos(2 * t));
                    if ((res - from).magnitude > 0.01f)
                    {
                        t = Mathf.PI / 4 - t;
                        direction = -new Vector2(-sign.x * Mathf.Sin(2 * t), sign.y * Mathf.Cos(2 * t));
                    }
                    res = new Vector2(sign.x * 0.5f * Mathf.Cos(2 * t), sign.y * 0.5f * Mathf.Sin(2 * t)) + center;
                    Debug.DrawLine(res, res + direction, Color.blue);
                }

                return res;
            }
        }
    }
}
