using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;
using System.Linq;

public class ProjectionManager : MonoBehaviour
{
    private CameraManager cameraManager;
    private NetworkManager networkManager;

    private Matrix4x4 cameraToWorldMatrix;
    private Matrix4x4 projectionMatrix;
    private GameObject m_Canvas;
    private byte[] HololensBytesJPG;

    private cameraParameters camParams;

    public Vector3 zAxisOverride;

    [SerializeField] public GameObject spherePrefab;

    // Start is called before the first frame update
    private void Awake()
    {
        cameraManager = GameObject.Find("MixedRealityPlayspace/Camera Manager").GetComponent<CameraManager>();
        networkManager = GameObject.Find("MixedRealityPlayspace/Network Manager").GetComponent<NetworkManager>();
        //currentFault = GameObject.Find("Fault").GetComponent<LAV_FaultMeasurement>();
    }

    private void Start()
    {
        
    }

    private void Update()
    {
    }

    public void saveCameraMatrix(Matrix4x4 c2w, Matrix4x4 proj, GameObject m_Canvas_argument, byte[] bytes)
    {
        cameraToWorldMatrix = c2w;
        projectionMatrix = proj;
        m_Canvas = m_Canvas_argument;
        HololensBytesJPG = bytes;

        camParams = calculateCameraParameters(cameraToWorldMatrix);
        zAxisOverride = -camParams.vz;

        //send image to server
        Debug.Log("Matrix data saved. Calling sendImage coroutine");
        StartCoroutine(networkManager.SendImageBlobDetect(HololensBytesJPG));
    }

    public void projectPoints(List<decimal[]> data)
    {
        drawCorners();
        Debug.DrawLine(camParams.focalPoint, camParams.vx, Color.red, 999f);
        Debug.DrawLine(camParams.focalPoint, camParams.vy, Color.green, 999f);
        Debug.DrawLine(camParams.focalPoint, camParams.vz, Color.blue, 999f);

        //Put these next rows for debug projection

        //SpawnOrb(camParams.focalPoint, Color.white);
        //Draw a grid of the image frame for debugging purposes

        List<Vector3> normals = new List<Vector3>();
        List<Vector3> points = new List<Vector3>();

        for (int y = 2196 / 4; y <= 2196 * 3 / 4; y += 100)
        {
            //the inner loop will control the x coordinate
            for (int x = 3904 / 4; x <= 3904 * 3 / 4; x += 100)
            {
                Debug.Log($"x,y = {x},{y}");
                addPointXY(x, y, Color.white);
                //RaycastNormal
                Vector3 targetPoint = ConvertUV2KyleXYZ(new Vector2(x, y));
                Vector3 normal = RaycastNormal(targetPoint);
                Vector3 point = RaycastPoint(targetPoint);
                normals.Add(normal);
                points.Add(point);
            }
        }
        Vector3 averageNormal = normals.Aggregate(Vector3.zero, (acc, v) => acc + v) / normals.Count;
        int cgy = 2196 / 2;
        int cgx = 3904 / 2;
        Vector3 cgPoint = ConvertUV2KyleXYZ(new Vector2(cgx, cgy));
        Vector3 cg = RaycastPoint(cgPoint);

        Vector3 averagePoint = points.Aggregate(Vector3.zero, (acc, v) => acc + v) / points.Count;
        addLinePoints(averagePoint, averagePoint + averageNormal*0.1f / averageNormal.magnitude, Color.magenta, 10);



        //Comment this line for debug projection
        RemoveAllSpherePrefabs();

        //Visualize the data
        string output = "Data unpacked: ";
        foreach (decimal[] arr in data)
        {
            output += "[";
            output += arr[0].ToString() + ", ";
            output += arr[1].ToString();
            output += "], ";
        }
        output = output.TrimEnd(',', ' ');
        Debug.Log(output);

        Vector4[] dataXYZD = new Vector4[data.Count-1];

        //Project each point received for visualization
        float avgDist = 0f;
        Vector3 avgPoint = Vector3.zero;
        for (int j = 0; j < data.Count - 1; j++)
        {
            float x = (float)data[j + 1][0];
            float y = (float)data[j + 1][1];
            Vector3 targetPoint = ConvertUV2KyleXYZ(new Vector2(x, y));
            Vector3 XYZ = RaycastPoint(targetPoint);
            float D = Vector3.Distance(XYZ, camParams.focalPoint);
            dataXYZD[j] = new Vector4(XYZ.x, XYZ.y, XYZ.z, D);
            avgDist += D;
            avgPoint += XYZ;
            SpawnOrb(XYZ, Color.red);
        }
    }

    public void drawCorners()
    {
        float w = 3904f;
        float h = 2196f;
        Vector2[] xyPoints = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(w,0),
            new Vector2(0,h),
            new Vector2(w,h)
        };

        foreach (Vector2 arr in xyPoints)
        {
            Vector3 canvasPoint = ConvertUV2KyleXYZ(new Vector2(arr.x, arr.y));
            Vector3 point = RaycastPoint(canvasPoint);
            SpawnOrb(point, Color.magenta);
            Debug.Log(string.Format("Adding point {0}", point.ToString("F5")));
            Vector3 p0 = RaycastPoint(point);
            addLinePoints(camParams.focalPoint, p0, Color.white, 20);
        }
    }

    public void addPointXY(float x, float y, Color c)
    {
        if (x == 0 && y == 0)
        {
            x = 3904f / 2;
            y = 2196f / 2;
        }

        Vector3 targetPoint = ConvertUV2KyleXYZ(new Vector2(x, y));
        Vector3 p0 = RaycastPoint(targetPoint);

        //Debug.Log(string.Format("Adding point {0} {1}", p0.ToString("F5"), new Vector3(-0.01f, 0, 0.1f).ToString("F5")));
        //currentFault.AddFaultPoint(p0);
        SpawnOrb(p0, c);

    }

    public void addLinePoints(Vector3 start, Vector3 end, Color c, int numPoints)
    {
        Vector3 dir = start - end;
        Vector3 step = dir / numPoints;
        for (int i = 0; i < numPoints; i++)
        {
            SpawnOrb(start - step*i, c);
        }
    }

    public Vector3 computeXYZ(float x = 0, float y = 0)
    {
        if (x == 0 && y == 0)
        {
            x = 3904f / 2;
            y = 2196f / 2;
        }

        Vector3 targetPoint = ConvertUV2KyleXYZ(new Vector2(x, y));
        //SpawnOrb(targetPoint, Color.magenta);
        //Debug.DrawLine(camParams.focalPoint, targetPoint, Color.magenta, 999f);

        Vector3 p0 = RaycastPoint(targetPoint);
        //SpawnOrb(p0, Color.red);

        return p0;

        //Debug.DrawLine(targetPoint, p0, Color.red, 999f);

        //Debug.Log(string.Format("Adding point {0} {1}", p0.ToString("F5"), new Vector3(-0.01f, 0, 0.1f).ToString("F5")));

        //Add the point to the bounding box data structure
        //currentFault.AddFaultPoint(p0, Quaternion.identity, new Vector3(-0.01f, 0, 0.1f));
    }

    public void SpawnOrb(Vector3 pos, Color c)
    {
        GameObject newOrb = Instantiate(spherePrefab);
        newOrb.transform.position = pos;
        //newOrb.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        newOrb.GetComponent<MeshRenderer>().material.SetColor("_Color", c);
    }

    public void SpawnOrbSize(Vector3 pos, Color c, float sz)
    {
        GameObject newOrb = Instantiate(spherePrefab);
        newOrb.transform.position = pos;
        newOrb.transform.localScale = new Vector3(sz, sz, sz);
        newOrb.GetComponent<MeshRenderer>().material.SetColor("_Color", c);
    }


    #region helper functions
    private Vector3 RaycastPoint(Vector3 p2)
    {
        Vector3 v = p2 - camParams.focalPoint;
        float dist = 10f;

        RaycastHit[] hits = Physics.RaycastAll(camParams.focalPoint, v, dist);
        RaycastHit hit = new RaycastHit();
        bool foundHit = false;

        //Take the closer raycast hit if there are multiple
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.gameObject.layer == 31)
            {
                if (foundHit)
                {
                    float distHit_i = Vector3.Distance(hits[i].point, camParams.focalPoint);
                    float distHit = Vector3.Distance(hit.point, camParams.focalPoint);
                    if (distHit_i < distHit)
                    {
                        hit = hits[i];
                    }
                }
                else
                {
                    hit = hits[i];
                    foundHit = true;
                }
            }
        }

        if (!foundHit)
        {
            Debug.Log("Did not find intersection.");
            hit.point = camParams.focalPoint - camParams.vz * 0.5f;
            hit.normal = camParams.vz;
        }
        else
        {
            hit.point = hit.point;// + hit.normal * 0.001f;
        }

        return hit.point;
    }

    private Vector3 RaycastNormal(Vector3 p2)
    {
        Vector3 v = p2 - camParams.focalPoint;
        float dist = 10f;

        RaycastHit[] hits = Physics.RaycastAll(camParams.focalPoint, v, dist);
        RaycastHit hit = new RaycastHit();
        bool foundHit = false;

        //Take the closer raycast hit if there are multiple
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.gameObject.layer == 31)
            {
                if (foundHit)
                {
                    float distHit_i = Vector3.Distance(hits[i].point, camParams.focalPoint);
                    float distHit = Vector3.Distance(hit.point, camParams.focalPoint);
                    if (distHit_i < distHit)
                    {
                        hit = hits[i];
                    }
                }
                else
                {
                    hit = hits[i];
                    foundHit = true;
                }
            }
        }

        if (!foundHit)
        {
            Debug.Log("Did not find intersection.");
            hit.point = camParams.focalPoint - camParams.vz * 0.5f;
            hit.normal = camParams.vz;
        }
        else
        {
            hit.point = hit.point;// + hit.normal * 0.001f;
        }

        return hit.normal;
    }

    //Returns the x,y image coordinates transformed into X,Y,Z coordinates on the projected canvas
    private Vector3 ConvertUV2KyleXYZ(Vector2 imgCoords)
    {
        float reducedWidth = 920f + 40f;//The last 40f is emperical bandaid
        float imageWidth = 3904f - reducedWidth;
        float imageHeight = 2196f;
        float U = (imgCoords.x - reducedWidth / 2) / imageWidth - 0.5f + 0.007f;//The last 0.007f is emperical bandaid
        float V = (imgCoords.y / imageHeight) * (0.35f + 0.39f) - 0.39f + 0.040f;//The last 0.040f is emperical bandaid
        Vector2 UV = new Vector2(U, V);
        Vector3 targetPoint = m_Canvas.transform.TransformPoint(new Vector3(UV.x, -UV.y, 0)); //Flip the y axis
        return targetPoint;
    }

    //This returns the X,Y,Z coordinates transforemd into x,y image coordinates
        //Can be used for backprojection onto the image. An example use case is when
        //a user edits ML generated points, they can be backprojected and recorded
        //as future training data
    private Vector2 ReverseKyleXYZ2UV(Vector3 targetPoint)
    {
        float reducedWidth = 920f + 40f;
        float imageWidth = 3904f - reducedWidth;
        float imageHeight = 2196f;
        Vector2 canvasCoords = m_Canvas.transform.InverseTransformPoint(targetPoint);
        float U = (canvasCoords.x + 0.5f - 0.007f) * imageWidth + reducedWidth / 2;
        float V = (-canvasCoords.y + 0.39f - 0.040f) / (0.35f + 0.39f) * imageHeight;//Flip the y axis
        Vector2 imgCoords = new Vector2(U, V);
        return imgCoords;
    }
    private cameraParameters calculateCameraParameters(Matrix4x4 c2w)
    {
        cameraParameters outParams = new cameraParameters();
        outParams.focalPoint = cameraToWorldMatrix.GetColumn(3);
        outParams.vx = cameraToWorldMatrix.GetColumn(0);
        outParams.vy = cameraToWorldMatrix.GetColumn(1);
        outParams.vz = cameraToWorldMatrix.GetColumn(2);

        outParams.imageW = 3904;
        outParams.imageH = 2196;
        //outParams.imageW = 640; //Webcam
        //outParams.imageH = 480;
        return outParams;
    }

    private class cameraParameters
    {
        public Vector3 vx;
        public Vector3 vy;
        public Vector3 vz;
        public Vector3 focalPoint;
        public float imageW;
        public float imageH;
    }
    #endregion

    public void RemoveAllSpherePrefabs()
    {
        // Get all GameObjects in the scene
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

        // Iterate over all GameObjects
        foreach (GameObject obj in allObjects)
        {
            // If the GameObject's name starts with "SpherePrefab", destroy it
            if (obj.name.StartsWith("SpherePrefab"))
            {
                Destroy(obj);
            }
        }
    }
}
