using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Permissions;

[System.Serializable]
public class PlannedTrajectory
{
    public Vector3 onSkin;
    public Vector3 onPericardium;
    public Vector3 normal;
}


public class PlannedDAVIDTrajectories : MonoBehaviour
{
    public GameObject phantom;
    public Transform cylinderPrefab;
     
   
    public GameObject end, start, circle_1, circle_2, circle_3;
    private GameObject cylinder;
    private Vector3 entry_skin_normal;
    private Vector3 target_pos;
    private Vector3 entry_skin_pos;
    public PlannedTrajectory t_first, t_second, t_third, t_fourth, t_fifth;
    private Quaternion traj_orientation;
    
    public TextMesh tr_debug;

    // Start is called before the first frame update
    void Start()
    {

        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        start.gameObject.SetActive(false);
        circle_1.gameObject.SetActive(false);
        circle_2.gameObject.SetActive(false);
        circle_3.gameObject.SetActive(false);

        //    initializing with planned trajectories
        t_first.normal = new Vector3(-0.09447481483221054f, 0.9901605248451233f, 0.1032303050160408f);        
        t_first.onSkin = new Vector3(16.156312942504883f, -121.84039306640625f, -74.2118911743164f);
        t_first.onPericardium = new Vector3(-22.421596151883485f, -36.92114855031498f, -43.59792678082576f);

        t_second.normal = new Vector3(-0.08987507969141006f, 0.9945082068443298f, 0.05362797528505325f);
        t_second.onSkin = new Vector3(34.69971466064453f, -120.3574447631836f, -67.22005462646484f);
        t_second.onPericardium = new Vector3(-23.758266595693737f, -64.55262664648203f, -22.773466770465557f);

        t_third.normal = new Vector3(-0.08236786723136902f, 0.9965842366218567f, 0.005950632970780134f);
        t_third.onSkin = new Vector3(4.59269380569458f, -124.71128845214844f, -50.873497009277344f);
        t_third.onPericardium = new Vector3(-12.698424864788443f, -64.6523109747439f, -43.17285856908681f);

        t_fourth.normal = new Vector3(-0.047670383006334305f, 0.9970386624336243f, 0.06034419685602188f);
        t_fourth.onSkin = new Vector3(5.378899574279785f, -124.18719482421875f, -62.31029510498047f);
        t_fourth.onPericardium = new Vector3(-32.355253596638526f, -54.28446268480877f, -38.4902106883914f);

        t_fifth.normal = new Vector3(-0.02707591839134693f, 0.9992154240608215f, 0.02890264242887497f);
        t_fifth.onSkin = new Vector3(-54.1551399230957f, -126.8061294555664f, -52.51538848876953f);
        t_fifth.onPericardium = new Vector3(-13.78656734890408f, -54.982720692952476f, -47.23779220581055f);
    }

    Vector3[] ConvertListToVector3Array(List<float> floatList)
    {
        if (floatList.Count % 3 != 0)
        {
            Debug.LogWarning("Invalid number of elements in the list. Each point should have three values (x, y, z).");
            return null;
        }

        Vector3[] vectorArray = new Vector3[floatList.Count / 3];

        for (int i = 0, j = 0; i < floatList.Count; i += 3, j++)
        {
            Vector3 point = new Vector3(-floatList[i], floatList[i + 1], floatList[i + 2]);
            vectorArray[j] = point;
        }

        return vectorArray;
    }
    public void InstantiateCylinder(Transform cylinderPrefab, Vector3 beginPoint, Vector3 endPoint)
    {
        if (cylinder != null)
        {
            Destroy(cylinder);
        }
        cylinder = Instantiate<GameObject>(cylinderPrefab.gameObject, Vector3.zero, Quaternion.identity);
        cylinder.transform.SetParent(phantom.transform);
        cylinder.name = "PlannedTrajectory";
        cylinder.gameObject.SetActive(false);
    }
    public void UpdateCylinderPosition(GameObject cylinder, Vector3 beginPoint, Vector3 endPoint)
    {

        Vector3 offset = (endPoint - beginPoint);
        Vector3 position = endPoint;
        Debug.Log($"offset{offset}");
        cylinder.transform.position = position;
        cylinder.transform.LookAt(beginPoint);
        Vector3 localScale = cylinder.transform.localScale;
        localScale.y = ((endPoint - beginPoint)).magnitude;
        localScale.x = 0.0035f;
        localScale.z = 0.0035f;
        cylinder.transform.Rotate(90.0f, 0.0f, 0.0f, Space.Self);
        cylinder.transform.localScale = localScale;
        cylinder.gameObject.SetActive(true);
        start.gameObject.SetActive(true);
        circle_1.gameObject.SetActive(true);
        circle_2.gameObject.SetActive(true);
        circle_3.gameObject.SetActive(true);
        Quaternion rotationToAlign = Quaternion.FromToRotation(circle_1.transform.TransformDirection(new Vector3(0, 0, 1)), cylinder.transform.up);
        circle_1.transform.position = cylinder.transform.position;
        circle_2.transform.position = cylinder.transform.position;
        circle_3.transform.position = cylinder.transform.position;


        circle_1.transform.rotation = rotationToAlign * circle_1.transform.rotation;
        circle_2.transform.rotation = rotationToAlign * circle_2.transform.rotation;
        circle_3.transform.rotation = rotationToAlign * circle_3.transform.rotation;
        // align to point normal on skin mesh
        Quaternion rotation_skin_target = Quaternion.LookRotation(Vector3.up, entry_skin_normal);
        start.transform.rotation = rotation_skin_target;

        circle_1.transform.Translate(new Vector3(0, 0, 1) * -0.01f, Space.Self);
        circle_2.transform.Translate(new Vector3(0, 0, 1) * -0.02f, Space.Self);
        circle_3.transform.Translate(new Vector3(0, 0, 1) * -0.03f, Space.Self);

    }
    public void Create_trajectory()
    {

        Debug.Log($"entry{entry_skin_pos},target{target_pos}, normal {entry_skin_normal}");

        target_pos = phantom.transform.TransformPoint(target_pos);
        entry_skin_pos = phantom.transform.TransformPoint(entry_skin_pos);
        entry_skin_normal = phantom.transform.TransformPoint(entry_skin_normal);
        entry_skin_normal.Normalize();

        start.transform.position = entry_skin_pos;
        end.transform.position = target_pos;

        Debug.Log($"entry{entry_skin_pos},target{target_pos}");

        UpdateCylinderPosition(cylinder, target_pos, entry_skin_pos);

    }

    public void FirstTrajectory()
    {

        entry_skin_pos.x = t_first.onSkin.x / 1000;
        entry_skin_pos.y = t_first.onSkin.y / 1000;
        entry_skin_pos.z = -t_first.onSkin.z / 1000;

        // just a vector - no need for scaling factor
        entry_skin_normal.x = t_first.normal.x;
        entry_skin_normal.y = t_first.normal.y;
        entry_skin_normal.z = -t_first.normal.z;

        target_pos.x = t_first.onPericardium.x / 1000;
        target_pos.y = t_first.onPericardium.y / 1000;
        target_pos.z = -t_first.onPericardium.z / 1000;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);
        
        Create_trajectory();

        tr_debug.text = "Selected: 1";
    }

    public void SecondTrajectory()
    {
        entry_skin_pos.x = t_second.onSkin.x / 1000;
        entry_skin_pos.y = t_second.onSkin.y / 1000;
        entry_skin_pos.z = -t_second.onSkin.z / 1000;

        // just a vector - no need for scaling factor
        entry_skin_normal.x = t_second.normal.x;
        entry_skin_normal.y = t_second.normal.y;
        entry_skin_normal.z = -t_second.normal.z;

        target_pos.x = t_second.onPericardium.x / 1000;
        target_pos.y = t_second.onPericardium.y / 1000;
        target_pos.z = -t_second.onPericardium.z / 1000;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);

        Create_trajectory();
        tr_debug.text = "Selected: 2";
    }

    public void ThirdTrajectory()
    {
        entry_skin_pos.x = t_third.onSkin.x / 1000;
        entry_skin_pos.y = t_third.onSkin.y / 1000;
        entry_skin_pos.z = -t_third.onSkin.z / 1000;

        // just a vector - no need for scaling factor
        entry_skin_normal.x = t_third.normal.x;
        entry_skin_normal.y = t_third.normal.y;
        entry_skin_normal.z = -t_third.normal.z;

        target_pos.x = t_third.onPericardium.x / 1000;
        target_pos.y = t_third.onPericardium.y / 1000;
        target_pos.z = -t_third.onPericardium.z / 1000;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);

        Create_trajectory();
        tr_debug.text = "Selected: 3";
     }

    public void FourthTrajectory()
    {
        entry_skin_pos.x = t_fourth.onSkin.x / 1000;
        entry_skin_pos.y = t_fourth.onSkin.y / 1000;
        entry_skin_pos.z = -t_fourth.onSkin.z / 1000;

        // just a vector - no need for scaling factor
        entry_skin_normal.x = t_fourth.normal.x;
        entry_skin_normal.y = t_fourth.normal.y;
        entry_skin_normal.z = -t_fourth.normal.z;

        target_pos.x = t_fourth.onPericardium.x / 1000;
        target_pos.y = t_fourth.onPericardium.y / 1000;
        target_pos.z = -t_fourth.onPericardium.z / 1000;
        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);

        Create_trajectory();
        tr_debug.text = "Selected: 4";
        
    }

    public void FifthTrajectory()
    {
        entry_skin_pos.x = t_fifth.onSkin.x / 1000;
        entry_skin_pos.y = t_fifth.onSkin.y / 1000;
        entry_skin_pos.z = -t_fifth.onSkin.z / 1000;

        // just a vector - no need for scaling factor
        entry_skin_normal.x = t_fifth.normal.x;
        entry_skin_normal.y = t_fifth.normal.y;
        entry_skin_normal.z = -t_fifth.normal.z;

        target_pos.x = t_fifth.onPericardium.x / 1000;
        target_pos.y = t_fifth.onPericardium.y / 1000;
        target_pos.z = -t_fifth.onPericardium.z / 1000;

        InstantiateCylinder(cylinderPrefab, target_pos, entry_skin_pos);

        Create_trajectory();
        tr_debug.text = "Selected: 5";
        
    }

    public void HideTrajectory()
    {   
        cylinder.gameObject.SetActive(false);
        circle_1.gameObject.SetActive(false);
        circle_2.gameObject.SetActive(false);
        circle_3.gameObject.SetActive(false);
    }
    public void ShowTrajectory()
    {
        cylinder.gameObject.SetActive(true);
        circle_1.gameObject.SetActive(true);
        circle_2.gameObject.SetActive(true);
        circle_3.gameObject.SetActive(true);
    }

}
