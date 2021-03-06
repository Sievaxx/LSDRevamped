using LSDR.Game;
using UnityEngine;
using LSDR.InputManagement;

namespace LSDR.Entities.Player
{
    /// <summary>
	/// Handles player motion. Moving forwards and backwards, and strafing if FPS movement is enabled.
	/// </summary>
	[RequireComponent(typeof (CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
	    public SettingsSystem Settings;
	    public ControlSchemeLoaderSystem ControlScheme;
	    
	    /// <summary>
		/// The speed at which to move. Set in editor.
		/// </summary>
        public float MovementSpeed;
		
		/// <summary>
		/// The multiplier on gravity. Affects falling speed. Set in editor.
		/// </summary>
        public float GravityMultiplier;

		/// <summary>
		/// The threshold above which to move the player when using a gamepad.
		/// </summary>
		public const float JOYSTICK_MOVE_THRESHOLD = 0.3F;

        private Vector3 _moveDir;
        private CharacterController _characterController;
        private CollisionFlags _collisionFlags;
        private bool _previouslyGrounded;

        // Use this for initialization
        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
        }

        // Update is called once per frame
        private void Update()
        {
	        _moveDir = getInput();
	        
	        if (!_characterController.isGrounded && _previouslyGrounded)
            {
                _moveDir.y = 0f;
            }

            _previouslyGrounded = _characterController.isGrounded;
        }

        private void FixedUpdate()
        {
	        // apply this movement direction to the forward and right vectors of the player
            var trans = transform;
            Vector3 desiredMove = trans.forward*_moveDir.y + trans.right*_moveDir.x;

            // get a normal for the surface that is being touched (if any) to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(trans.position, _characterController.radius, Vector3.down, out hitInfo,
                               _characterController.height/2f, ~0, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            // modify the current movement direction to include these new calculated values
            Vector3 moveDirection = new Vector3(desiredMove.x*MovementSpeed, 0, desiredMove.z*MovementSpeed);

            // if we're not grounded we want to apply gravity to this movement direction, for falling
            if (!_characterController.isGrounded)
            {
				moveDirection += Physics.gravity * (GravityMultiplier * Time.fixedDeltaTime);
			}
            
            // move the character controller
            _collisionFlags = _characterController.Move(moveDirection*Time.fixedDeltaTime);
        }

        /// <summary>
        /// Get the input movement vector from the control scheme.
        /// </summary>
        /// <returns>Movement direction.</returns>
        private Vector2 getInput()
        {
			// if we can't control the player return zero for input direction
	        if (!Settings.CanControlPlayer) return Vector2.zero;
	        // get vector axes from input system
            float moveDirFrontBack = ControlScheme.Current.Actions.MoveY;
	        float moveDirLeftRight = ControlScheme.Current.FpsControls ? ControlScheme.Current.Actions.MoveX : 0f;
            Vector2 input = new Vector2(moveDirLeftRight, moveDirFrontBack);

            // normalize input if it exceeds 1 in combined length (for diagonal movement)
            if (input.sqrMagnitude > 1)
            {
                input.Normalize();
            }

            return input;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (_collisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(_characterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
