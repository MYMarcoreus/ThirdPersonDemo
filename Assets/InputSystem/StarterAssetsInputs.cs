using System;
using Manager;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Yy.Protocol.App;

namespace InputSystem
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;



#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
			Debug.Log("OnMove Pressed");
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
			Debug.Log("OnLook Pressed");
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
			Debug.Log("OnJump Pressed");
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
			Debug.Log("OnSprint Pressed");
		}

		public void OnEscape(InputValue value)
		{
			Debug.Log("Escape Button Pressed");
		}
		
		
		
		
		public void InputMove(InputAction.CallbackContext context)
		{
			MoveInput(context.ReadValue<Vector2>());
		}

		public void InputLook(InputAction.CallbackContext context)
		{
			if (cursorInputForLook)
			{
				LookInput(context.ReadValue<Vector2>());
			}
		}

		public void InputJump(InputAction.CallbackContext context)
		{
			JumpInput(context.ReadValueAsButton());
		}

		public void InputSprint(InputAction.CallbackContext context)
		{
			SprintInput(context.ReadValueAsButton());
		}

		public void InputEscape(InputAction.CallbackContext context)
		{
			if (context.started)
			{
				Debug.Log($"Escape Button Pressed {context.action}");
				// playerInput.SwitchCurrentActionMap("UI");
				
				UIManager.instance.SetDefaultPanel(UIPanelType.eRoom);
				SelfQuitRoomReq req = new SelfQuitRoomReq
				{
					RoomId = RoomDataManager.instance.room_data.RoomId,
					Uid = PlayerDataManager.instance.SelfData.basedata.AccountData.Uid,
					UserToken = NetworkManager.instance.user_token
				};
				NetworkManager.instance.client_gate.RegisterMessageCallback<SelfQuitRoomRsp>(OnSelfQuitRoomRsp);
				NetworkManager.instance.client_gate.CreateTcpPackage(req);
			}
		}

		void OnSelfQuitRoomRsp(SelfQuitRoomRsp rsp)
		{
			switch (rsp.ResultCode)
			{
				case SelfQuitRoomRsp.Types.Status.ESuccess:
					print($"OnSelfQuitRoomRsp: {rsp}");
					print($"PlayerData: {PlayerDataManager.instance.SelfData.basedata.AccountData}");
					print($"加载Login场景");
					AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("Scenes/Login");
					if (asyncOperation != null)
					{
						asyncOperation.completed += (aO) =>
						{
							print($"Load Login Scene");
							print($"PlayerData: {PlayerDataManager.instance.SelfData.basedata.AccountData}");
							SetCursorState(false);
							NetworkManager.instance.ResetLogicClient();
							RoomDataManager.instance.room_data = null;
							PlayerDataManager.instance.ResetOtherDatas();
						};
					}

					break;
				case SelfQuitRoomRsp.Types.Status.ERoomNotExist:
					break;
				case SelfQuitRoomRsp.Types.Status.EUnknownError:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}