//Arduino code borrowed from http://arduino.cc/en/Tutorial/Sweep

#include <Servo.h>
Servo xservo; 
int pos = 0;
boolean moveArm = false;
int serialState = 0;

void setup(){
  xservo.attach(14); //(analog pin 0) for the x servo 
  Serial.begin(9600); // 9600 is the rate of communication 
}

void loop()
{
  if(moveArm)
  {
    for(pos = 0; pos < 180; pos += 1)  // goes from 0 degrees to 180 degrees
    {                                  // in steps of 1 degree
      xservo.write(pos);              // tell servo to go to position in variable 'pos'
      delay(5);                       // waits 15ms for the servo to reach the position
    }
    for(pos = 180; pos>=1; pos-=1)     // goes from 180 degrees to 0 degrees
    {                                
      xservo.write(pos);              // tell servo to go to position in variable 'pos'
      delay(5);                       // waits 15ms for the servo to reach the position
    }
    xservo.write(90);
    moveArm = false;
  }
  delay(100);
}

void serialEvent()
{
  while(Serial.available()) {
    char inChar = (char)Serial.read();
    switch(serialState)
    {
      case 0: if(inChar == 'm') serialState++; break;
      case 1: if(inChar == 'o') serialState++; break;        
      case 2: if(inChar == 'v') serialState++; break;
      case 3: if(inChar == 'e') serialState = 0; moveArm = true; break;
      default: serialState = 0; break;
    }
  }
}

