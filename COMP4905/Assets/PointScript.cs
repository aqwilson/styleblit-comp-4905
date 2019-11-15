 using UnityEngine;
 using System.Collections;
 public class PointScript : MonoBehaviour
 {

     public int random = 12;
     ParticleSystem.Particle[] cloud;
     bool bPointsUpdated = false;

     void Start() {
        // this.GetComponent<ParticleSystem>().Play();
     }
     
     void Update () 
     {
        //  if (bPointsUpdated)
        //  {
            // Debug.Log(this.GetComponent<ParticleSystem>().IsAlive());
            //  this.GetComponent<ParticleSystem>().SetParticles(cloud, cloud.Length);
            // this.GetComponent<ParticleSystem>().Play();
            //  this.GetComponent<ParticleSystem>().Emit();
        //      bPointsUpdated = false;
        //  }
     }
     
     public void SetPoints(Vector3[] positions)
     {        
         cloud = new ParticleSystem.Particle[positions.Length];
         
         for (int ii = 0; ii < positions.Length; ++ii)
         {
             cloud[ii].position = positions[ii];            
             cloud[ii].color = Color.red;
             cloud[ii].size = 0.1f;            
         }
 
         bPointsUpdated = true;
     }
 }