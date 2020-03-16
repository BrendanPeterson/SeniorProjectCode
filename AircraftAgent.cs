using MLAgents;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Aircraft
{
    public class AircraftAgent : Agent
    {
        [Header("Movement Parameters")]
        public float thrust = 100000f;
        public float pitchSpeed = 100f;
        public float yawSpeed = 100f;
        public float rollSpeed = 100f;
        public float boostMultiplier = 2f;

        [Header("Explosion Stuff")]
        [Tooltip("The aircraft mesh that will dissapear on exploding")]
        public GameObject meshObject;

        [Tooltip("Game object of the explosion particle effectr")]
        public GameObject explosionEffect;

        [Header(" Training")]
        [Tooltip(" Number of steps to timeout after in training if not doing what its supposed to")]
        public int stepTimeout = 300;


        public int NextCheckpointIndex { get; set; }

        //Components to keep track of
        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;
        private RayPerception3D rayPerception;

        // When the next step timeout will be during training
        private float nextStepTimeout;

        //Whether aircraft is frozen (intentionally not flying) eg. paused game
        private bool frozen = false;



        //Controls
        private float pitchChange = 0f;
        private float smoothPitchChange = 0f;
        private float maxPitchAngle = 45f;
        private float yawChange = 0f;
        private float smoothYawChange = 0f;
        private float rollChange = 0f;
        private float smoothRollChange = 0f;
        private float maxRollAngle = 45f;
        private bool boost;

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();
            rayPerception = GetComponent<RayPerception3D>();

            //Override max step set in inspoector
            //Max 5000 if training infinite if racing
            agentParameters.maxStep = area.trainingMode ? 5000 : 0;
        }

        ///<summary>
        ///Read Action Inpumts from Vector Action
        ///</summary>
        ///<param name="vectorAction"> the chosen actions </param>
        ///<param name="textAction"> the chosen text action (unused) </param>
        public override void AgentAction(float[] vectorAction, string textAction)
        {
            //Read values for pitch and yaw
            pitchChange = vectorAction[0]; // up or none 
            if (pitchChange == 2) pitchChange = -1f; //down


            yawChange = vectorAction[1]; //trun right or none
            if (yawChange == 2) yawChange = -1f; //turn left
           

            //Read value for bosst and enable/disable trail renderer
            boost = vectorAction[2] == 1;
            if (boost & !trail.emitting) trail.Clear();
            trail.emitting = boost;

            //If frozen go back so agent doesnt move
            if (frozen) return;

            ProcessMovement();

            if (area.trainingMode)
            {
                //Small negative reward every Step
                AddReward(-1f / agentParameters.maxStep);

                //Make sure we havent run out of training time
                //Add negative reward if the agent runs out of time
                if (GetStepCount() > nextStepTimeout)
                {
                    AddReward(-.5f);
                    Done();
                }

                Vector3 localCheckpointDir = VectorToNextCheckpoint();
                //Creates curriculum learning by shrinking how close the agent hast to get to a checkpoint to receive a reward
                if (localCheckpointDir.magnitude < area.AircraftAcademy.resetParameters["checkpoint_radius"])
                {
                    GotCheckpoint();
                }
            }
        }

        /// <summary>
        /// Collect observations uised by agent to make decisions
        /// </summary>

        public override void CollectObservations()
        {
            //Observe aircraftr velocity (1 vector3 = 3 values)
            AddVectorObs(transform.InverseTransformDirection(rigidbody.velocity));

            //Where is the next checkpoint
            AddVectorObs(VectorToNextCheckpoint());

            //Orientation of next checkpoint (1 vector 3 = 3values)
            Vector3 nextCheckpointForward = area.Checkpoints[NextCheckpointIndex].transform.forward;
            AddVectorObs(transform.InverseTransformDirection(nextCheckpointForward));


            //Observe rayperception results
            string[] detectableObjects = { "Untagged", "checkpoint" };

            //Look ahead and upward
            //(2 tags + 1hit/not + 1 distance to obj) * 3 ray angles = 12 values
            AddVectorObs(rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 90f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: 75f
            ));

            //Look center and at several angles along the horizon
            //(2 tags + 1hit/not + 1 distance to obj) * 7 ray angles = 28 values
            AddVectorObs(rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 70f, 80f, 90f, 100f, 110f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: 0f
            ));

            //Look ahead and downward
            //(2 tags + 1hit/not + 1 distance to obj) * 3 ray angles = 12 values
            AddVectorObs(rayPerception.Perceive(
                rayDistance: 250f,
                rayAngles: new float[] { 60f, 90f, 120f },
                detectableObjects: detectableObjects,
                startOffset: 0f,
                endOffset: -75f
            ));

            //Total observations = 3 + 3 + 3 + 12 + 28 + 12 = 61

        }

        public override void AgentReset()
        {
            //Reset the velocity, position and orientation
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            trail.emitting = false;
            area.ResetAgentPosition(agent: this, randomize: area.trainingMode);

            //Update the step timeout if training
            if (area.trainingMode) nextStepTimeout = GetStepCount() + stepTimeout;
        }

        /// <summary>
        /// prevent the agent from moving and taking actions
        /// </summary>
        public void FreezeAgent()
        {
            Debug.Assert(area.trainingMode == false, " Freeze/Thaw not supported in training");
            frozen = true;
            rigidbody.Sleep();
            trail.emitting = false;
        }

        public void ThawAgent()
        {
            Debug.Assert(area.trainingMode == false, " Freeze/Thaw not supported in training");
            frozen = false;
            rigidbody.WakeUp();
        }

        /// <summary>
        /// called when agent flies through correct checkpoint
        /// </summary>
        private void GotCheckpoint()
        {
            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.Checkpoints.Count;

            if (area.trainingMode)
            {
                AddReward(.5f);
                nextStepTimeout = GetStepCount() + stepTimeout;
            }
        }

        /// <summary>
        /// Gets a vector to the next checkpoint the agent needs to fly through
        /// </summary>
        /// <returns>A local space vector</returns>
        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;
            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
            return localCheckpointDir;
        }

        //Calculate and apply movement;
        private void ProcessMovement()
        {
            //Calculate boost
            float boostModifier = boost ? boostMultiplier : 1f;

            //Apply forward thrust
            rigidbody.AddForce(transform.forward * thrust * boostModifier, ForceMode.Force);

            //Get current rotation
            Vector3 curRot = transform.rotation.eulerAngles;

            //Roll Angle Claculation (between -180 and 180)
            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;
            if (yawChange == 0f)
            {
                //Not turning; Smoothly roll toward center;
                rollChange = -rollAngle / maxRollAngle;
            }else
            {
                //Trurning; roll in opposite direction of turn
                rollChange = -yawChange;
            }

            //calculate smooth deltas
            smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

            //Calculate new pitch, yaw, and roll. Clamp pitch and roll.
            float pitch = ClampAngle(curRot.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed,
                                        -maxPitchAngle,
                                         maxPitchAngle);
            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
            float roll = ClampAngle(curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed,
                                        -maxRollAngle,
                                        maxRollAngle);

            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }
        ///<summary>
        ///Clamps an angle between two values 
        /// </summary>
        ///<param name="angle"The input angle></param>
        ///<param name="from">Lower limit</param>
        ///<param name ="to">Upper Limit</param>
        ///<returns></returns>

        private static float ClampAngle(float angle, float from, float to)
        {
            if (angle < 0f) angle = 360f + angle;
            if (angle > 180f) return Mathf.Max(angle, 360f + from);
            return Mathf.Min(angle, to);
        }

        /// <summary>
        /// React to entering a trigger
        /// </summary>
        /// <param name="other">collider entered</param>
        private void OnTriggerEnter(Collider other)
        {
            if ( other.transform.CompareTag("checkpoint") &&
                 other.gameObject == area.Checkpoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }
        }

        /// <summary>
        /// React to collisioons
        /// </summary>
        /// <param name="collision">Collision info</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.transform.CompareTag("agent"))
            {
                //We hit something that wasnt another agnet
                if (area.trainingMode)
                {
                    AddReward(-1f);
                    Done();
                    return;
                }
                else
                {
                    StartCoroutine(ExplosionReset());
                }
            }
        }

        /// <summary>
        /// Reset aircraft th the most recent complete checkpoint
        /// </summary>
        /// <returns>yield return</returns>
        private IEnumerator ExplosionReset()
        {
            FreezeAgent();

            //Disable aircraft mesh object enable explosion
            meshObject.SetActive(false);
            explosionEffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            //Disable aircraft explosion reenable mesh
            meshObject.SetActive(true);
            explosionEffect.SetActive(false);
            area.ResetAgentPosition(agent: this);
            yield return new WaitForSeconds(1f);

            ThawAgent();
        }
    }
}

