using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class gameHandler : MonoBehaviour
{    
    public List<GameObject> objects;
    public List<GameObject> changingObjects;
    
    public List<Material> myMaterials;
    public Text MyText;

    List<GameObject> disappearingObjects;
    List<GameObject> appearingObjects;

    List<string> disappearedObject = new List<string>();
    List<string> appearedObject = new List<string>();

    List<GameObject> changedObject = new List<GameObject>();

    List<int[]> changesPerLevel = new List<int[]>();

    int currentLevel = 0;
    int extraClickCount = 0;
    int totDifferences = 0;
    int differencesFound = 0;
    int numberOfLevels = 0;
    bool levelStarted = false;

    void Start()
    {   
        /* 
        Read number of changes per level from config file

        - each row is a level, 
        - col 1 number of disappearing objects
        - col 2 number of appearing objects
        - col 3 number of changing color objects
        
         */

        StreamReader fin = new StreamReader("config.txt");
        string inputString;

        int row = 0;

        while ((inputString = fin.ReadLine()) != null)
        {
            string[] read =  inputString.Split(char.Parse(" "));

            int[] changes = new int[3];

            for(int col=0; col<3; col++)
                changes[col] = int.Parse(read[col]);


            changesPerLevel.Add(changes);
           
            row += 1;
        }

        numberOfLevels = row;

        fin.Close();

        totDifferences = changesPerLevel[0][0] + changesPerLevel[0][1] + changesPerLevel[0][2];
        MyText.text = "0/"+totDifferences;


        /* 
        Starting scene
        Half of the objects will disappear randomly. 
        These objects are initially set invisible and saved in "appearingObjects" in order to be able to reappear.

        The remaining objects are saved in "disappearingObjects"

        */ 

        appearingObjects = new List<GameObject>();
        disappearingObjects = new List<GameObject>();

        int half = objects.Count/2;
        
        while ( appearingObjects.Count < half ) {

            int randomIndex = Random.Range( 0, objects.Count );

            appearingObjects.Add( objects[randomIndex] );

            setVisibilityObject(objects[randomIndex], false);
            objects.RemoveAt(randomIndex);
        }

        disappearingObjects = objects;


        StartCoroutine(ChangeRandomObject());
    }



    IEnumerator ChangeRandomObject()
    {   
        MyText.text = "Starting level " + (currentLevel+1);

        yield return new WaitForSeconds(15);

        this.GetComponentInChildren<Image>().enabled = true;

        totDifferences = changesPerLevel[currentLevel][0] + changesPerLevel[currentLevel][1] + changesPerLevel[currentLevel][2];
        differencesFound = 0;
        MyText.text = "0/"+totDifferences;


        // Remove objects randomly by setting them invisible
        for(int i=0; i<changesPerLevel[currentLevel][0]; i++){

            int randomIndex = Random.Range(0, disappearingObjects.Count - i);

            GameObject g = disappearingObjects[randomIndex];

            disappearedObject.Add(g.transform.gameObject.name);

            setVisibilityObject(g, false);
            
            // Sposto l'elemento alla fine in modo da evitare di selezionarlo alla prossima iterazione
            disappearingObjects.RemoveAt(randomIndex);
            disappearingObjects.Add(g);

        }

        
        // Add objects randomly by setting them visible
        for(int i=0; i<changesPerLevel[currentLevel][1]; i++){

            int randomIndex = Random.Range(0, appearingObjects.Count - i);

            GameObject g = appearingObjects[randomIndex];

            appearedObject.Add(g.transform.gameObject.name);

            setVisibilityObject(g, true);
            
            appearingObjects.RemoveAt(randomIndex);
            appearingObjects.Add(g);
        }
        

        // Change color of objects randomly
        for(int i=0; i<changesPerLevel[currentLevel][2]; i++){
            
            int randomIndex = Random.Range(0,changingObjects.Count - i);

            GameObject g = changingObjects[randomIndex];

            /*Cambio il materiale corrente in myMaterials, in questo modo posso alternare fra i due colori*/
            Material currentMaterial = g.GetComponent<MeshRenderer>().material;
            g.GetComponent<MeshRenderer>().material = myMaterials[randomIndex];

            changedObject.Add(g);

            changingObjects.RemoveAt(randomIndex);
            changingObjects.Add(g);

            myMaterials.RemoveAt(randomIndex);
            myMaterials.Add(currentMaterial);
        }

        

        yield return new WaitForSeconds(3);

        levelStarted = true;

        this.GetComponentInChildren<Image>().enabled = false;

        // Prints of changes
        Debug.Log(("Level "+currentLevel));

        string dis ="Disappeared:";
        foreach(string s in disappearedObject)
            dis += " " + s;

        string ap ="Appeared:";
        foreach(string s in appearedObject)
            ap += " " + s;
        
        string ch = "Changed:";
        foreach(GameObject s in changedObject)
            ch += " " +s.name;

        Debug.Log(dis);
        Debug.Log(ap);
        Debug.Log(ch);
    }


    /*True: visible object, False: invisible object*/
    void setVisibilityObject(GameObject g, bool b){

        g.GetComponent<MeshRenderer>().enabled = b;
        foreach(Transform child in g.transform)
        {
            child.gameObject.GetComponent<MeshRenderer>().enabled = b;
        }

    }



    // Update is called once per frame
    void Update()
    {
        if (levelStarted && Input.GetMouseButtonDown(0) && differencesFound < totDifferences){
            Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
            RaycastHit hit;

            bool found = false;
            
            if( Physics.Raycast( ray, out hit, 100 ) )
            {
                
                if(disappearedObject.Contains(hit.transform.gameObject.name)){

                    setVisibilityObject(hit.transform.gameObject, true);
                    disappearedObject.Remove(hit.transform.gameObject.name);

                    found = true;


                }else if(appearedObject.Contains(hit.transform.gameObject.name)){

                    setVisibilityObject(hit.transform.gameObject, false);
                    appearedObject.Remove(hit.transform.gameObject.name);

                    found = true;


                }else if(changedObject.Contains(hit.transform.gameObject)){//.name)){

                    int index = changingObjects.IndexOf(hit.transform.gameObject);//.name);;

                    Material currentMaterial = changingObjects[index].GetComponent<MeshRenderer>().material;
                    changingObjects[index].GetComponent<MeshRenderer>().material = myMaterials[index];
                    myMaterials[index] = currentMaterial;

                    changedObject.Remove(hit.transform.gameObject);
                    found = true;

                }

            }

            if(found){
                differencesFound += 1;
                MyText.text = differencesFound + "/"+ totDifferences;

                if(changedObject.Count==0 && appearedObject.Count==0 && disappearedObject.Count==0){
                    Debug.Log("All differences found!");
                    levelStarted = false;

                    if(currentLevel == numberOfLevels-1){
                            MyText.text = "Game finished. Extra clicks: " + extraClickCount;
                    }
                    else{
                        currentLevel += 1;
                        StartCoroutine(ChangeRandomObject());
                    }
                }

            }else{
                extraClickCount += 1;
                Debug.Log("extra clicks: " + extraClickCount);
            }     
            
        }

        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
        
        
    }
}
