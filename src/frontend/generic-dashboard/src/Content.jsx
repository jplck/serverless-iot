import React from 'react';

class Content extends React.Component {

    render() {
        return (
            <div>
                <div>Token: {this.props.apiToken}</div>
                <div><a href="#" onClick={this.props.logout}>Logout</a></div>
                <p>
                    Machine Temperature {this.props.telemetry.temperature}
                </p>
                <p>
                    Machine Pressure: {this.props.telemetry.pressure}
                </p>
                <p>
                    Ambient Temperature: {this.props.telemetry.temperature}
                </p>
                <p>
                    Ambient Humidity: {this.props.telemetry.humidity}
                </p>
                <p>
                    Measurement Date: {this.props.telemetry.measurementDate}
                </p>
            </div>
        )
    }
    
}

export default Content;