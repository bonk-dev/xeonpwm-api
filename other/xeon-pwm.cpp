// XEON PWM MCU CODE

#include <Preferences.h>

const int SERIAL_BAUD_RATE = 115200;

const int PWM_FAN_PIN = 4;
const int DEFAULT_PWM_CHANNEL = 0;
const int DEFAULT_PWM_FREQ = 25000;
const int DEFAULT_PWM_RESOLUTION = 8;
const int DEFAULT_DUTY_CYCLE = 170;

#define MAX_DUTY_CYCLE(res) static_cast<uint32_t>(pow(2, res) - 1)
const int DEFAULT_MAX_DUTY_CYCLE = MAX_DUTY_CYCLE(DEFAULT_PWM_RESOLUTION);

const char* SETTING_NAMESPACE = "xeon-pwm";
const char* SETTING_PWM_FREQ = "pwm-frequency";
const char* SETTING_PWM_CHANNEL = "pwm-channel";
const char* SETTING_PWM_RES = "pwm-resolution";
const char* SETTING_PWM_PIN = "pwm-fan-pin";

uint32_t current_resolution = DEFAULT_PWM_RESOLUTION;
uint32_t current_duty_cycle = DEFAULT_DUTY_CYCLE;
uint32_t current_pin = PWM_FAN_PIN;
bool enable_debug = false;
Preferences preferences;

void print_settings(uint32_t ch, uint32_t freq, uint32_t res, uint32_t pin, bool parseable) {
  if (parseable) {
    cmd_write(ch);
    cmd_write('|');
    cmd_write(freq);
    cmd_write('|');
    cmd_write(res);
    cmd_write('|');
    cmd_write(pin);
    cmd_write('|');
    cmd_write_ln(MAX_DUTY_CYCLE(res));
  }
  else {
    cmd_write("PWM Channel: ");
    cmd_write_ln(ch);

    cmd_write("PWM Frequency: ");
    cmd_write(freq);
    cmd_write_ln(" Hz");

    cmd_write("PWM Resolution: ");
    cmd_write_ln(res);

    cmd_write("PWM GPIO pin: ");
    cmd_write_ln(pin);

    cmd_write("PWM Max duty cycle: ");
    cmd_write_ln(MAX_DUTY_CYCLE(res));
  }
}

/* COMMANDS */
/** DEFINED **/
// RESTART| - soft-restarts the MCU
// SET_DT_CYCLE|duty_cycle(int) - set the PWM duty cycle
// GET_DT_CYCLE| - get the PWM duty cycle
// SET_PWM_SETTING|[FREQUENCY,CHANNEL,RESOLUTION,PIN](string)|value(uint) - set a PWM data (requires a power cycle)
// SHOW_PWM_SETTINGS|[PRETTY](str,optional) - display all PWM settings (if PRETTY is present, then prints the settings in a more readable format)
// RESET_PWM_SETTINGS| - reset all the settings to their default values

/** RESULTS **/
const uint32_t ERR_SUCCESS = 0;
const uint32_t ERR_INVALID_COMMAND = 1;
const uint32_t ERR_PWM_INVALID_SETTING = 2;
const uint32_t ERR_PWM_INVALID_NAN = 3;
const uint32_t ERR_PWM_DISABLED = 4;

/** COMMANDS UTILITIES **/
// Reads an integer for use in command handlers
// (Maybe implement other ways than Serial)
long cmd_read_int() {
  return Serial.parseInt();
}

String cmd_read_arg() {
  return Serial.readStringUntil('|');
}

void cmd_debug_log(String str) {
  if (enable_debug) {
    Serial.print(str);
  }
}
void cmd_debug_logln(String str) {
  if (enable_debug) {
    Serial.println(str);
  }
}
void cmd_debug_log(uint32_t intr) {
  if (enable_debug) {
    Serial.print(intr);
  }
}
void cmd_debug_logln(uint32_t intr) {
  if (enable_debug) {
    Serial.println(intr);
  }
}

void cmd_write_result(uint32_t result) {
  Serial.println(result);
}
void cmd_write(String str) {
  Serial.print(str);
}
void cmd_write_ln(String str) {
  Serial.println(str);
}
void cmd_write(char c) {
  Serial.print(c);
}
void cmd_write_ln(char c) {
  Serial.println(c);
}
void cmd_write(uint32_t value) {
  Serial.print(value);
}
void cmd_write_ln(uint32_t value) {
  Serial.println(value);
}

/** HANDLERS **/
uint32_t handle_set_dt_cycle() {
  current_duty_cycle = cmd_read_int();
  cmd_debug_log("Read duty_cycle: ");
  cmd_debug_logln(current_duty_cycle);

  if (current_duty_cycle < 0) {
    current_duty_cycle = 0;
  }
  else if (current_duty_cycle > MAX_DUTY_CYCLE(current_resolution)) {
    current_duty_cycle = MAX_DUTY_CYCLE(current_resolution);
  }

  cmd_debug_log("Current duty_cycle: ");
  cmd_debug_logln(current_duty_cycle); 

  return ERR_SUCCESS;
}

uint32_t handle_get_dt_cycle() {
  cmd_write_ln(current_duty_cycle);

  return ERR_SUCCESS;
}

uint32_t handle_set_pwm_setting() {
  String setting = cmd_read_arg();
  if (setting == NULL) {
    return ERR_PWM_INVALID_SETTING;
  }

  uint32_t value = static_cast<uint32_t>(cmd_read_int());
  if (setting == "FREQUENCY") {
    preferences.putUInt(SETTING_PWM_FREQ, value);
  }
  else if (setting == "CHANNEL") {
    preferences.putUInt(SETTING_PWM_CHANNEL, value);
  }
  else if (setting == "RESOLUTION") {
    preferences.putUInt(SETTING_PWM_RES, value);
    current_resolution = value;
  }
  else if (setting == "PIN") {
    //preferences.putUInt(SETTING_PWM_PIN, value);
    // very easy to brick the program
    return ERR_PWM_DISABLED;
  }
  else {
    return ERR_PWM_INVALID_SETTING;
  }

  return ERR_SUCCESS;
}

uint32_t handle_reset_pwm_settings() {
  preferences.clear();
  return ERR_SUCCESS;
}

uint32_t handle_show_pwm_settings() {
  uint32_t pwm_channel = preferences.getUInt(SETTING_PWM_CHANNEL, DEFAULT_PWM_CHANNEL);
  uint32_t pwm_frequency = preferences.getUInt(SETTING_PWM_FREQ, DEFAULT_PWM_FREQ);
  uint32_t pwm_resolution = preferences.getUInt(SETTING_PWM_RES, DEFAULT_PWM_RESOLUTION);
  uint32_t pwm_fan_pin = preferences.getUInt(SETTING_PWM_PIN, PWM_FAN_PIN);

  String arg = cmd_read_arg();
  print_settings(
    pwm_channel, 
    pwm_frequency, 
    pwm_resolution, 
    pwm_fan_pin, 
    (arg == NULL || arg != "PRETTY"));

  return ERR_SUCCESS;
}

uint32_t handle_restart() {
  preferences.end();
  ledcDetachPin(current_pin);
  ESP.restart();
  return ERR_SUCCESS;
}

void serial_flush(){
  while(Serial.available() > 0) {
    char t = Serial.read();
  }
}

void handle_commands() {
  if (Serial.available() > 0) {
    String cmd = cmd_read_arg();
    uint32_t result = 0;

    // leave CMDs as Strings for readability
    if (cmd == "SET_DT_CYCLE") {
      result = handle_set_dt_cycle();
    }
    else if (cmd == "GET_DT_CYCLE") {
      result = handle_get_dt_cycle();
    }
    else if (cmd == "SET_PWM_SETTING") {
      result = handle_set_pwm_setting();
    }
    else if (cmd == "RESET_PWM_SETTINGS") {
      result = handle_reset_pwm_settings();
    }
    else if (cmd == "SHOW_PWM_SETTINGS") {
      result = handle_show_pwm_settings();
    }
    else if (cmd == "RESTART") {
      result = handle_restart();
    }
    else if (cmd != NULL) {
      result = ERR_INVALID_COMMAND;
      serial_flush();
    }

    cmd_write_result(result);
  }
}

/* ARDUINO functions */
void setup() {
  Serial.begin(SERIAL_BAUD_RATE);

  preferences.begin(SETTING_NAMESPACE, false);
  uint32_t pwm_channel = preferences.getUInt(SETTING_PWM_CHANNEL, DEFAULT_PWM_CHANNEL);
  uint32_t pwm_frequency = preferences.getUInt(SETTING_PWM_FREQ, DEFAULT_PWM_FREQ);
  uint32_t pwm_resolution = preferences.getUInt(SETTING_PWM_RES, DEFAULT_PWM_RESOLUTION);
  uint32_t pwm_fan_pin = preferences.getUInt(SETTING_PWM_PIN, PWM_FAN_PIN);

  current_pin = pwm_fan_pin;
  current_resolution = pwm_resolution;

  print_settings(
    pwm_channel, 
    pwm_frequency, 
    pwm_resolution, 
    pwm_fan_pin, 
    true);

  ledcSetup(pwm_channel, pwm_frequency, pwm_resolution);
  ledcAttachPin(pwm_fan_pin, pwm_channel);
}

void loop() {
  handle_commands();
  ledcWrite(DEFAULT_PWM_CHANNEL, current_duty_cycle);
}
