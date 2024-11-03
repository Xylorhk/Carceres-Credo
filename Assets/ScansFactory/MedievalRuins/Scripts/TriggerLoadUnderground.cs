namespace TriggerLoadUnderground
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.SceneManagement;
    using UnityEngine;

    public class TriggerLoadUnderground : MonoBehaviour
    {
        public GameObject[] objectsToShow;
        public GameObject[] objectsToHide;


        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                for (int i = 0; i < objectsToShow.Length; i++)
                {
                    objectsToShow[i].SetActive(true);
                }

                for (int i = 0; i < objectsToHide.Length; i++)
                {
                    objectsToHide[i].SetActive(false);
                }


            }

        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                for (int i = 0; i < objectsToShow.Length; i++)
                {
                    objectsToShow[i].SetActive(false);
                }

                for (int i = 0; i < objectsToHide.Length; i++)
                {
                    objectsToHide[i].SetActive(true);
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
