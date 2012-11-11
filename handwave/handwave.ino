//Arduino code borrowed from http://arduino.cc/en/Tutorial/Sweep

#include <Servo.h>
Servo xservo; 
int pos = 0;

void setup(){
  xservo.attach(14); //(analog pin 0) for the x servo 
//  Serial.begin(19200); // 19200 is the rate of communication 
}

void loop()
{
  for(pos = 0; pos < 180; pos += 1)  // goes from 0 degrees to 180 degrees
  {                                  // in steps of 1 degree
    xservo.write(pos);              // tell servo to go to position in variable 'pos'
    delay(15);                       // waits 15ms for the servo to reach the position
  }
  for(pos = 180; pos>=1; pos-=1)     // goes from 180 degrees to 0 degrees
  {                                
    xservo.write(pos);              // tell servo to go to position in variable 'pos'
    delay(15);                       // waits 15ms for the servo to reach the position
  }
}

