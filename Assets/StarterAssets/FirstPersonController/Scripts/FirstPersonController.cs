using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
using Cinemachine;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
	[RequireComponent(typeof(PlayerInput))]
#endif

	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float RealTimeSpeed = 0.0f;
		public float MoveSpeed = 6.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 12.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		public float SlidingSpeedIncrease = 6.0f;
		public float SlidingSpeedDecrease = 2.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;
		public float WallGravity = -1.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;
		public int JumpStep = 2;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;
		public LayerMask wallMask; // Mask of wall

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		public TMP_Text textTime;
		public TMP_Text textFinishTime;

		// Camera effects
		public CinemachineVirtualCamera playerCamera;
		private float normalFov;
		public float specialFov;
		public float tilt;
		public float wallRunTilt;
		public float cameraChangeTime;
		public GameObject playerModel;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;
		private float startHeight;

		private Vector3 forwardDirection;

		private bool isSprinting;
		private bool isCrouching;

		private bool isWallRunning;
		private bool onLeftWall;
		private bool onRightWall;
		bool hasWallRun = false;
		private RaycastHit leftWallHit;
		private RaycastHit rightWallHit;
		private Vector3 wallNormal;
		private Vector3 lastWall;

		private bool isSliding;
		private float slidingSpeed;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;
		private int _jumpStep;
		
		private Transform _transform;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
				return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
			}
		}

		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
			_playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
			_jumpStep = JumpStep;

			_transform = GetComponent<Transform>();

			startHeight = _transform.localScale.y;

			normalFov = playerCamera.m_Lens.FieldOfView;
		}

		private void Update()
		{
			textTime.text = "Time: " + Data.instance.time.ToString("f2") + " s";
			textFinishTime.text = "Total game time is " + Data.instance.time.ToString("f2") + " s";
			if (MenuController.instance.finish)
			{
				if (Input.GetKeyDown(KeyCode.R))
				{
					MenuController.instance.HideMenu();
					Data.instance.time = 0;
					SceneManager.LoadScene("Level 1");
				
				}
			}
			
			if (Input.GetKeyDown(KeyCode.P))
			{
				if (MenuController.instance.gameObject.activeSelf)
				{
					MenuController.instance.HideMenu();
				}
				else
				{
					MenuController.instance.ShowPauseMenu();
				}
				
			}
			if (MenuController.instance.isPause)
			{
				return;
			}
			//Cursor.lockState = CursorLockMode.None;
			JumpAndGravity();
			GroundedCheck();
			CheckWallRun(); // Check if is wallrunning
			CheckCrouch();

			if(isSliding && _speed > MoveSpeed)
            {
				slidingSpeed -= SlidingSpeedDecrease * Time.deltaTime * 10;
				
				//Debug.Log("Slide Timer = " + slideTimer);
				//if (slideTimer <= 0) isSliding = false;
				if (slidingSpeed < MoveSpeed + 0.5f)
				{
					//isSliding = false;
					//isCrouching = true;
					slidingSpeed = MoveSpeed;
				}
            }

			if (!Grounded && isWallRunning)
			{
				WallRunMovement();
			}
			CameraEffects();
			Move();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
			if (Grounded)
			{
				hasWallRun = false;
			}
		}

		private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			if (isCrouching)
			{
				_speed = MoveSpeed;
			}
			if (isSliding)
			{
				_speed = slidingSpeed;
			}
			

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			Vector3 direction = inputDirection;
			if (isWallRunning)
			{
				_verticalVelocity *= WallGravity;
				direction = forwardDirection;
			}
			//Debug.Log(_speed);
			RealTimeSpeed = _speed;
			// move the player
			_controller.Move(direction.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;
				_jumpStep = 2;
				hasWallRun = false;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
					_jumpStep--;
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				if (Input.GetKeyDown(KeyCode.Space) && _jumpStep > 0)
				{
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
					_jumpStep--;
				}

				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;

			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}


		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}

		private void CheckWallRun()
		{
			//Debug.Log("Check wall run!");
			onRightWall = Physics.Raycast(transform.position, transform.right, out rightWallHit, 0.7f, wallMask);
			onLeftWall = Physics.Raycast(transform.position, -transform.right, out leftWallHit, 0.7f, wallMask);

			if ((onRightWall || onLeftWall) && !isWallRunning)
			{
				TestWallRun();
			}
			else if (!onRightWall && !onLeftWall && isWallRunning)
			{
				ExitWallRun();
			}
		}

		private void WallRunMovement()
		{
			//Debug.Log("Wall run movement!");
			if (_input.move.y < (forwardDirection.z - 10f) && _input.move.y > (forwardDirection.z + 10f) || Input.GetKeyDown(KeyCode.Space))
			{
				ExitWallRun();
			}
		}

		private void TestWallRun()
        {
			//Debug.Log("Test wall run!");
			wallNormal = onRightWall ? rightWallHit.normal : leftWallHit.normal;
			if (!wallNormal.Equals(lastWall)) lastWall = wallNormal;
			if (hasWallRun)
			{
				WallRun();
            }
            else
            {
				hasWallRun = true;
				WallRun();
            }
		}

		private void WallRun()
        {
			//Debug.Log("In wall run!");
			isWallRunning = true;

			forwardDirection = Vector3.Cross(wallNormal, Vector3.up);

			if (Vector3.Dot(forwardDirection, transform.forward) < 0)
			{
				forwardDirection = -forwardDirection;
			}

		}

		private void ExitWallRun()
        {
			//Debug.Log("Exit wall run!");
			isWallRunning = false;
			lastWall = wallNormal;
			forwardDirection = wallNormal;
			_jumpStep = 2;
		}
		
		private void CheckCrouch()
		{
			if (Input.GetKey(KeyCode.C))
			{
				isCrouching = true;
				Crouch();
            }
            else
            {
				isCrouching = false;
				isSliding = false;
				_transform.localScale = new Vector3(transform.localScale.x, startHeight, transform.localScale.z);
			}
			
		}

		/*
		private void CheckSprint()
        {
            if (Input.GetKey(KeyCode.LeftShift) && !isCrouching)
            {
				isSprinting = true;
				_speed = SprintSpeed;
            }
            else
            {
				isSprinting = false;
				_speed = MoveSpeed;
			}
        }
		*/

		private void Crouch()
        {
			isCrouching = true;
			_transform.localScale = new Vector3(transform.localScale.x, 0.6f, transform.localScale.z);
			//Debug.Log("In crouch speed = " + _speed + " and sprintspeed - 1 = " + (SprintSpeed - 1.0f));
			if (_speed > SprintSpeed - 0.5f && !isSliding)
			{
				isSliding = true;
				if (Grounded)
				{
					slidingSpeed = SprintSpeed + SlidingSpeedIncrease;
					_speed = slidingSpeed;
				}
			}
		}

		private void CameraEffects()
        {
			float fov = isWallRunning ? specialFov : normalFov;
			playerCamera.m_Lens.FieldOfView = Mathf.Lerp(playerCamera.m_Lens.FieldOfView, fov, 60 * Time.deltaTime);
			float x = playerCamera.transform.localEulerAngles.x;
			float y = playerCamera.transform.localEulerAngles.y;
			float tx = playerModel.transform.localEulerAngles.x;
			float ty = playerModel.transform.localEulerAngles.y;
			float cameraDistance = playerCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>().CameraDistance;

			if (isWallRunning)
            {
                if (onRightWall)
                {
					playerCamera.m_Lens.Dutch = wallRunTilt;

					playerModel.transform.localEulerAngles = new Vector3(tx, ty, wallRunTilt);
					playerCamera.transform.localEulerAngles = new Vector3(x, y, wallRunTilt);
					playerCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>().CameraDistance = Mathf.Lerp(cameraDistance, 3, cameraChangeTime * Time.deltaTime);
				}
				else if (onLeftWall)
                {
					playerModel.transform.localEulerAngles = new Vector3(tx, ty, -wallRunTilt);
					//playerModel.transform.position = new Vector3(lastWall.x, playerModel.transform.position.y, playerModel.transform.position.z);
					playerCamera.m_Lens.Dutch = -wallRunTilt;
					playerCamera.transform.localEulerAngles = new Vector3(x, y, -wallRunTilt);
					playerCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>().CameraDistance = Mathf.Lerp(cameraDistance, 3, cameraChangeTime * Time.deltaTime);
				}
            }
            else
            {
				playerCamera.m_Lens.Dutch = 0;
				playerCamera.transform.localEulerAngles = new Vector3(x, y, 0f);
				playerModel.transform.localEulerAngles = new Vector3(tx, ty, 0f);
				playerCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>().CameraDistance = Mathf.Lerp(cameraDistance, 0, cameraChangeTime * Time.deltaTime);
			}
        }
	}
}