﻿using FlatRedBall.Glue.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.PlatformerPlugin.ViewModels;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    class CodeGenerator : ElementComponentCodeGenerator
    {
        public override CodeLocation CodeLocation
        {
            get
            {
                return CodeLocation.AfterStandardGenerated;
            }
        }
        public override void GenerateAdditionalClasses(ICodeBlock codeBlock, IElement element)
        {
            ///////////////Early Out//////////////////////
            if(GetIfIsPlatformer(element) == false)
            {
                return;
            }
            /////////////End Early Out////////////////////

            codeBlock.Enum("public", "MovementType")
                .Line("Ground,")
                .Line("Air,")
                .Line("AfterDoubleJump");

            codeBlock.Enum("public", "HorizontalDirection")
                .Line("Left,")
                .Line("Right");
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            ///////////////Early Out//////////////////////
            if (GetIfIsPlatformer(element) == false)
            {
                return codeBlock;
            }
            /////////////End Early Out////////////////////



            codeBlock.Line("#region Platformer Fields");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// See property for information.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("bool mIsOnGround = false;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether the character has hit its head on a solid");
            codeBlock.Line("/// collision this frame. This typically occurs when the");
            codeBlock.Line("/// character is moving up in the air. It is used to prevent");
            codeBlock.Line("/// upward velocity from being applied while the player is");
            codeBlock.Line("/// holding down the jump button.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("bool mHitHead = false;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether the character is in the air and has double-jumped.");
            codeBlock.Line("/// This is used to determine which movement variables are active,");
            codeBlock.Line("/// effectively preventing multiple double-jumps.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("bool mHasDoubleJumped = false;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The time when the jump button was last pushed. This is used to");
            codeBlock.Line("/// determine if upward velocity should be applied while the user");
            codeBlock.Line("/// holds the jump button down.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("double mTimeJumpPushed = double.NegativeInfinity;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The MovementValues which were active when the user last jumped.");
            codeBlock.Line("/// These are used to determine the upward velocity to apply while");
            codeBlock.Line("/// the user holds the jump button.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("DataTypes.PlatformerValues mValuesJumpedWith;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// See property for information.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("DataTypes.PlatformerValues mCurrentMovement;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// See property for information.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("HorizontalDirection mDirectionFacing;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// See property for information.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("MovementType mMovementType;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The last time collision checks were performed. Time values uniquely");
            codeBlock.Line("/// identify a game frame, so this is used to store whether collisions have");
            codeBlock.Line("/// been tested this frame or not. This is used to determine whether collision");
            codeBlock.Line("/// variables should be reset or not when a collision method is called, as");
            codeBlock.Line("/// multiple collisions (such as vs. solid and vs. cloud) may occur in one frame.");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("double mLastCollisionTime = -1;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Denotes what type of collision has occurred");
            codeBlock.Line("/// </summary>");
            codeBlock.Line("protected enum PlatformerCollisionType { None, GroundCollision, HeadCollision }");
            codeBlock.Line("protected delegate PlatformerCollisionType CustomPlatformerCollisionCheckDelegate(float previousY, float currentY);");
            codeBlock.Line("private CustomPlatformerCollisionCheckDelegate mCustomCollisionCheckDelegate = null;");
            codeBlock.Line("protected void SetCustomPlatformerCollisionCheck(CustomPlatformerCollisionCheckDelegate customCheck)");
            codeBlock.Line("{");
            codeBlock.Line("mCustomCollisionCheckDelegate = customCheck;");
            codeBlock.Line("}");

            codeBlock.Line("#endregion");




            codeBlock.Line("#region Platformer Properties");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Returns the current time, considering whether a Screen is active. ");
            codeBlock.Line("/// This is used to control how long a user can hold the jump button during");
            codeBlock.Line("/// a jump to apply upward velocity.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("double", "CurrentTime")
                .Get()
                    .If("FlatRedBall.Screens.ScreenManager.CurrentScreen != null")
                        .Line("return FlatRedBall.Screens.ScreenManager.CurrentScreen.PauseAdjustedCurrentTime;")
                    .End()
                    .Else()
                        .Line("return FlatRedBall.TimeManager.CurrentTime;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The current movement variables used for horizontal movement and jumping.");
            codeBlock.Line("/// These automatically get set according to the default platformer logic and should");
            codeBlock.Line("/// not be manually adjusted.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("protected DataTypes.PlatformerValues", "CurrentMovement")
                .Get()
                    .Line("return mCurrentMovement;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Which direciton the character is facing.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("protected HorizontalDirection", "DirectionFacing")
                .Get()
                    .Line("return mDirectionFacing;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The input object which controls whether the jump was pressed.");
            codeBlock.Line("/// Common examples include a button or keyboard key.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public FlatRedBall.Input.IPressableInput", "JumpInput");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The input object which controls the horizontal movement of the character.");
            codeBlock.Line("/// Common examples include a d-pad, analog stick, or keyboard keys.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public FlatRedBall.Input.I1DInput", "HorizontalInput");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The ratio that the horizontal input is being held.");
            codeBlock.Line("/// -1 represents full left, 0 is neutral, +1 is full right.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("protected virtual float", "HorizontalRatio")
                .Get()
                    .If("!InputEnabled")
                        .Line("return 0;")
                    .End()
                    .Else()
                        .Line("return HorizontalInput.Value;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether the character is on the ground. This is false");
            codeBlock.Line("/// if the character has jumped or walked off of the edge");
            codeBlock.Line("/// of a platform.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("public bool", "IsOnGround")
                .Get()
                    .Line("return mIsOnGround;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// The current movement type. This is set by the default platformer logic and");
            codeBlock.Line("/// is used to assign the mCurrentMovement variable.");
            codeBlock.Line("/// </summary>");
            codeBlock.Property("public MovementType", "CurrentMovementType")
                .Get()
                    .Line("return mMovementType;")
                .End()
                .Set()
                    .Line("mMovementType = value;")
                    .Line("UpdateCurrentMovement();")
                    
                    .If("CurrentMovement != null")
                        .Line("this.YAcceleration = -CurrentMovement.Gravity;");

            codeBlock.Line("/// <summary>");
            codeBlock.Line("/// Whether input is read to control the movement of the character.");
            codeBlock.Line("/// This can be turned off if the player should not be able to control");
            codeBlock.Line("/// the character.");
            codeBlock.Line("/// </summary>");
            codeBlock.AutoProperty("public bool", "InputEnabled");

            // This can be used to add anything else here without the complexity of CodeBlock calls
            codeBlock.Line(
@"


");




            codeBlock.Line("#endregion");

            codeBlock.Line(
@"
        /// <summary>
        /// Action for when the character executes a jump.
        /// </summary>
        public System.Action JumpAction;

        /// <summary>
        /// Action for when the character lands from a jump.
        /// </summary>
        public System.Action LandedAction;

");
            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if(!GetIfIsPlatformer(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            codeBlock.Line(
@"
            // this provides default controls for the platformer using either keyboad or 360. Can be overridden in custom code:
            this.InitializeInput();


            AfterGroundMovementSet += (not, used) => UpdateCurrentMovement();
            AfterAirMovementSet += (not, used) => UpdateCurrentMovement();
            AfterAfterDoubleJumpSet += (not, used) => UpdateCurrentMovement();
");
            return codeBlock;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if(!GetIfIsPlatformer(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            codeBlock.Line("CurrentMovementType = MovementType.Ground;");
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if(!GetIfIsPlatformer(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            #region Huge Code Block

            codeBlock.Line(
@"

        private void UpdateCurrentMovement()
        {
            switch (mMovementType)
            {
                case MovementType.Ground:
                    mCurrentMovement = GroundMovement;
                    break;
                case MovementType.Air:
                    mCurrentMovement = AirMovement;
                    break;
                case MovementType.AfterDoubleJump:
                    mCurrentMovement = AfterDoubleJump;
                    break;
            }
        }
        #region Platformer Methods
        /// <summary>
        /// Sets the HorizontalInput and JumpInput instances to either the keyboard or 
        /// Xbox360GamePad index 0. This can be overridden by base classes to default
        /// to different input devices.
        /// </summary>
        protected virtual void InitializeInput()
        {
            if (FlatRedBall.Input.InputManager.Xbox360GamePads[0].IsConnected)
            {
                this.JumpInput =
                    FlatRedBall.Input.InputManager.Xbox360GamePads[0].GetButton(FlatRedBall.Input.Xbox360GamePad.Button.A);

                this.HorizontalInput =
                    FlatRedBall.Input.InputManager.Xbox360GamePads[0].LeftStick.Horizontal;
            }
            else
            {
                this.JumpInput =
                    FlatRedBall.Input.InputManager.Keyboard.GetKey(Microsoft.Xna.Framework.Input.Keys.Space);

                this.HorizontalInput =
                    FlatRedBall.Input.InputManager.Keyboard.Get1DInput(Microsoft.Xna.Framework.Input.Keys.Left, Microsoft.Xna.Framework.Input.Keys.Right);
            }

            InputEnabled = true;
        }


        /// <summary>
        /// Reads all input and applies the read-in values to control
        /// velocity and character state.
        /// </summary>
        private void ApplyInput()
        {
            ApplyHorizontalInput();

            ApplyJumpInput();
        }

        /// <summary>
        /// Applies the horizontal input to control horizontal movement and state.
        /// </summary>
        private void ApplyHorizontalInput()
        {
            float horizontalRatio = HorizontalRatio;


            if(horizontalRatio > 0)
            {
                mDirectionFacing = HorizontalDirection.Right;
            }
            else if(horizontalRatio < 0)
            {
                mDirectionFacing = HorizontalDirection.Left;
            }

            if (this.CurrentMovement.AccelerationTimeX <= 0)
            {
                this.XVelocity = horizontalRatio * CurrentMovement.MaxSpeedX;
            }
            else
            {
                float acceleration = CurrentMovement.MaxSpeedX / CurrentMovement.AccelerationTimeX;

                float sign = System.Math.Sign(horizontalRatio);
                float magnitude = System.Math.Abs(horizontalRatio);

                if(sign == 0)
                {
                    sign = -System.Math.Sign(XVelocity);
                    magnitude = 1;
                }

                if (XVelocity == 0 || sign == System.Math.Sign(XVelocity))
                {
                    this.XAcceleration = sign * magnitude * CurrentMovement.MaxSpeedX / CurrentMovement.AccelerationTimeX;
                }
                else
                {
                    float accelerationValue = magnitude * CurrentMovement.MaxSpeedX / CurrentMovement.DecelerationTimeX;


                    if (System.Math.Abs(XVelocity) < accelerationValue * FlatRedBall.TimeManager.SecondDifference)
                    {
                        this.XAcceleration = 0;
                        this.XVelocity = 0;
                    }
                    else
                    {

                        // slowing down
                        this.XAcceleration = sign * accelerationValue;
                    }

                }

                XVelocity = System.Math.Min(XVelocity, CurrentMovement.MaxSpeedX);
                XVelocity = System.Math.Max(XVelocity, -CurrentMovement.MaxSpeedX);
            }
        }

        /// <summary>
        /// Applies the jump input to control vertical velocity and state.
        /// </summary>
        private void ApplyJumpInput()
        {
			bool jumpPushed = JumpInput.WasJustPressed && InputEnabled;
			bool jumpDown = JumpInput.IsDown && InputEnabled;


            if (jumpPushed && 
                //CurrentMovement.JumpVelocity > 0 &&
                (mIsOnGround || AfterDoubleJump == null || 
				(AfterDoubleJump != null && mHasDoubleJumped == false) ||
				(AfterDoubleJump != null && AfterDoubleJump.JumpVelocity > 0)

				)
                
                )
            {

                mTimeJumpPushed = CurrentTime;
                this.YVelocity = CurrentMovement.JumpVelocity;
                mValuesJumpedWith = CurrentMovement;

                if (JumpAction != null)
                {
                    JumpAction();
                }

                if (CurrentMovementType == MovementType.Air)
                {
                    mHasDoubleJumped = true ;
                }
            }

            double secondsSincePush = CurrentTime - mTimeJumpPushed;

            if (mValuesJumpedWith != null && 
                secondsSincePush < mValuesJumpedWith.JumpApplyLength &&
				(mValuesJumpedWith.JumpApplyByButtonHold == false || JumpInput.IsDown)
                )
            {
                this.YVelocity = mValuesJumpedWith.JumpVelocity;

            }

            if (mValuesJumpedWith != null && mValuesJumpedWith.JumpApplyByButtonHold &&
				(!JumpInput.IsDown || mHitHead)
                )
            {
                mValuesJumpedWith = null;
            }

            if (CurrentMovement.MaxFallSpeed < 0)
            {
                this.YVelocity = System.Math.Min(-CurrentMovement.MaxFallSpeed, this.YVelocity);
            }
            else
            {
                this.YVelocity = System.Math.Max(-CurrentMovement.MaxFallSpeed, this.YVelocity);
            }
        }


        /// <summary>
        /// Performs a standard solid collision against a ShapeCollection.
        /// </summary>
        /// <param name=""shapeCollection""></param>
        public void CollideAgainst(FlatRedBall.Math.Geometry.ShapeCollection shapeCollection)
        {
            CollideAgainst(shapeCollection, false);
        }

        public void CollideAgainst(FlatRedBall.Math.Geometry.AxisAlignedRectangle rectangle, bool isCloudCollision = false)
        {
            CollideAgainst(() => rectangle.CollideAgainstBounce(this.Collision, 1, 0, 0), isCloudCollision);
        }

        /// <summary>
        /// Performs a solid or cloud collision against a ShapeCollection.
        /// </summary>
        /// <param name=""shapeCollection"">The ShapeCollection to collide against.</param>
        /// <param name=""isCloudCollision"">Whether to perform solid or cloud collisions.</param>
        public void CollideAgainst(FlatRedBall.Math.Geometry.ShapeCollection shapeCollection, bool isCloudCollision)
        {
            CollideAgainst(() => shapeCollection.CollideAgainstBounce(this.Collision, 1, 0, 0), isCloudCollision);
        }

        /// <summary>
        /// Executes the collisionFunction to determine if a collision occurred, and if so, reacts
        /// to the collision by modifying the state of the object and raising appropriate events.
        /// This is useful for situations where custom collisions are needed, but then the standard
        /// behavior is desired if a collision occurs.
        /// </summary>
        /// <param name=""collisionFunction"">The collision function to execute.</param>
        /// <param name=""isCloudCollision"">Whether to perform cloud collision (only check when moving down)</param>
        public void CollideAgainst(System.Func<bool> collisionFunction, bool isCloudCollision)
        {
            Microsoft.Xna.Framework.Vector3 positionBeforeCollision = this.Position;
            Microsoft.Xna.Framework.Vector3 velocityBeforeCollision = this.Velocity;

            float lastY = this.Y;

            bool isFirstCollisionOfTheFrame = FlatRedBall.TimeManager.CurrentTime != mLastCollisionTime;

            if (isFirstCollisionOfTheFrame)
            {
                mLastCollisionTime = FlatRedBall.TimeManager.CurrentTime;
                mIsOnGround = false;
                mHitHead = false;
            }

            if (isCloudCollision == false || velocityBeforeCollision.Y < 0)
            {

                if (collisionFunction())
                {
                    // make sure that we've been moved up, and that we're falling
                    bool shouldApplyCollision = true;
                    if (isCloudCollision)
                    {
                        if (this.Y <= positionBeforeCollision.Y)
                        {
                            shouldApplyCollision = false;
                        }
                    }

                    if (shouldApplyCollision)
                    {
                         if (mCustomCollisionCheckDelegate != null)
                        {
                            var result = mCustomCollisionCheckDelegate(lastY, Y);
                            switch (result)
                            {
                                case PlatformerCollisionType.GroundCollision:
                                    if (!mIsOnGround && LandedAction != null)
                                    {
                                        LandedAction();
                                    }
                                    mIsOnGround = true;
                                    break;

                                case PlatformerCollisionType.HeadCollision:
                                    mHitHead = true;
                                    break;

                                default:
                                    throw new System.NotSupportedException($""Collision type of {result} is not supported"");
                            }
                        }
                        else
                        {
                            if (this.Y > lastY)
                            {
                                if (!mIsOnGround && LandedAction != null)
                                {
                                    LandedAction();
                                }
                                mIsOnGround = true;
                            }
                            if (this.Y < lastY)
                            {
                                mHitHead = true;
                            }
                        }
}
                    else
                    {
                        Position = positionBeforeCollision;
                        Velocity = velocityBeforeCollision;
                    }
                }
            }
        }

        /// <summary>
        /// Assigns the current movement values based off of whether the user is on ground and has double-jumped or not.
        /// This is called automatically, but it can be overridden in derived classes to perform custom assignment of 
        /// movement types.
        /// </summary>
        protected virtual void DetermineMovementValues()
        {
            if (mIsOnGround)
            {
                mHasDoubleJumped = false;
                if (CurrentMovementType == MovementType.Air ||
                    CurrentMovementType == MovementType.AfterDoubleJump)
                {
                    CurrentMovementType = MovementType.Ground;
                }
            }
            else
            {
                if (CurrentMovementType == MovementType.Ground)
                {
                    CurrentMovementType = MovementType.Air;
                }

            }

            if (CurrentMovementType == MovementType.Air && mHasDoubleJumped)
            {
                CurrentMovementType = MovementType.AfterDoubleJump;
            }



        }

        #endregion
");
            #endregion

            return base.GenerateAdditionalMethods(codeBlock, element);
        }


        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, IElement element)
        {
            ///////////////////Early Out///////////////////////////////
            if(!GetIfIsPlatformer(element))
            {
                return codeBlock;
            }
            /////////////////End Early Out/////////////////////////////

            codeBlock.Line(
@"
            ApplyInput();

            DetermineMovementValues();
");


            return base.GenerateActivity(codeBlock, element);
        }

        bool GetIfIsPlatformer(IElement element)
        {
            return element.Properties
                .GetValue<bool>(nameof(PlatformerEntityViewModel.IsPlatformer));
        }
    }
}
