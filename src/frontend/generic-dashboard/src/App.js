import React from 'react';
import { UserAgentApplication } from 'msal'
import config from './config'
import { LogLevel, HubConnectionBuilder } from '@aspnet/signalr';
import Content from './Content'

class App extends React.Component {
  constructor(props) {
    super(props)

    this.login = this.login.bind(this)
    this.logout = this.logout.bind(this)

    this.agent = new UserAgentApplication({
      auth: {
        clientId: process.env.REACT_APP_APP_ID
      },
      cache: {
        cacheLocation: "localStorage",
        storeAuthStateInCookie: true
      }
    })

    this.state = {
      isAuthenticated: false,
      apiToken: '',
      signalrConnected: false,
      scopes: [config.graphScope, process.env.REACT_APP_API_SCOPE],
      machine: 
      {
        temperature: 0.0,
        pressure: 0.0
      },
      ambient: 
      {
        temperature: 0.0,
        humidity: 0.0
      },
      measurementDate: (new Date()).toISOString()
    }
  }

  componentDidMount() {
    this.init()
  }

  async init() {
    var user = this.agent.getAccount()
    if (user !== null) {
      var token = await this.getSignalrToken()
      if (token !== null) {
        this.connectSignalr(token)
        this.setState({
          isAuthenticated: true,
          apiToken: token,
          signalrConnected: true
        })
      }
    } 
  }

  async login() {
    try {
      await this.agent.loginPopup(
        {
          scopes: this.state.scopes,
          prompt: "select_account"
        }
      )
      await this.init()
    }
    catch(err) {
      //TODO: Add APP insights
      console.log(err)
    }
  }

  logout() {
    this.agent.logout();
  }

  connectSignalr(token) {
    var hubConnectionRef = new HubConnectionBuilder()
        .withUrl(process.env.REACT_APP_HUB_URL, {accessTokenFactory: () => {
          return token.accessToken
        }
      })
      .configureLogging(LogLevel.Debug)
      .build()

    hubConnectionRef.start()

    hubConnectionRef.onclose(function() {
      //TODO: Add APP insights
      console.log('signalr disconnected')
    })

    hubConnectionRef.on(config.signalrPipeName, (telemetryJson) => {
      var telemetry = JSON.parse(telemetryJson)
      this.setState({
        machine: 
        {
          temperature: telemetry.machine.temperature,
          pressure: telemetry.machine.pressure
        },
        ambient: 
        {
          temperature: telemetry.ambient.pressure,
          humidity: telemetry.ambient.humidity
        },
        measurementDate: telemetry.timeCreated
      })
    })

  }

  async getSignalrToken() {
    try {
      var accessToken = await this.agent.acquireTokenSilent({
        scopes: [ process.env.REACT_APP_API_SCOPE ]
      })

      if (accessToken) {
        return accessToken
      }
    }
    catch (err) {
      //TODO: Add APP insights
      return null
    }
  }

  render() {
    var content
    if (this.state.isAuthenticated) {
      content = <Content apiToken={this.state.apiToken.accessToken} machine={this.state.machine} ambient={this.state.ambient} logout={this.logout} />
    } else {
      content = <div><a href="#" onClick={this.login}>Login</a></div>
    }

    return (
      <div className="App">
        {content}
      </div>
    )
  }
  
}

export default App;
